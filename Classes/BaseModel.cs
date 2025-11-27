using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace StoreManager.Classes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DbFieldAttribute : Attribute
    {
        public string? ColumnName { get; }
        public DbFieldAttribute(string? columnName = null)
        {
            ColumnName = columnName;
        }
    }

    [AttributeUsage(AttributeTargets.Property)]
    public class AutoIncrementAttribute : Attribute { }


    public abstract class BaseModel<TSelf> where TSelf: BaseModel<TSelf>, new()
    {
        protected readonly List<string> _edited = [];

        public BaseModel() { }

        protected abstract string TableName { get; }
        protected static string ValidateInput(string input, int length)
        {
            input = input.Trim();
            if (input.Length > length)
                throw new ArgumentException("Too long");
            return input;
        }

        private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _dbFieldsCache = new();

        protected PropertyInfo[] GetDbFields()
        {
            return _dbFieldsCache.GetOrAdd(GetType(), t =>
                t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(p => Attribute.IsDefined(p, typeof(DbFieldAttribute)))
                .ToArray()
            );
        }

        private static readonly ConcurrentDictionary<Type, PropertyInfo?> _autoFieldCache = new();

        private PropertyInfo? GetAutoIncrementField()
        {
            return _autoFieldCache.GetOrAdd(GetType(), t =>
                t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                 .FirstOrDefault(p => Attribute.IsDefined(p, typeof(AutoIncrementAttribute)))
            );
        }

        public static async Task<List<TSelf>> FromReaderAsync(DbDataReader reader)
        {
            var list = new List<TSelf>();
            var fields = new TSelf().GetDbFields();

            var map = fields.ToDictionary(
                p => (p.GetCustomAttribute<DbFieldAttribute>()?.ColumnName ?? p.Name).ToLower(),
                p => p
            );

            while (await reader.ReadAsync())
            {
                var obj = new TSelf();

                for (int i = 0; i < reader.FieldCount; i++)
                {
                    string col = reader.GetName(i).ToLower();
                    if (!map.TryGetValue(col, out var prop))
                        continue;

                    object? value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                    prop.SetValue(obj, value);
                }

                list.Add(obj);
            }

            return list;
        }

        protected void Set<T>(ref T field, T value, [CallerMemberName] string propName = "")
        {
            if (!EqualityComparer<T>.Default.Equals(field, value))
            {
                field = value;
                _edited.Add(propName.ToLower());
            }
        }
        public static async Task<TSelf?> FromFirstRowAsync(DbDataReader reader)
        {
            if (!await reader.ReadAsync())
                return null;

            var obj = new TSelf();
            var fields = obj.GetDbFields();

            var map = fields.ToDictionary(
                p => (p.GetCustomAttribute<DbFieldAttribute>()?.ColumnName ?? p.Name).ToLower(),
                p => p
            );

            for (int i = 0; i < reader.FieldCount; i++)
            {
                string col = reader.GetName(i).ToLower();
                if (!map.TryGetValue(col, out var prop))
                    continue;

                object? value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
                prop.SetValue(obj, value);
            }

            return obj;
        }

        public async Task Save()
        {
            // adatok
            var autoField = GetAutoIncrementField();
            var fields = GetDbFields().Where(p => p != autoField);

            var columns = string.Join(", ", fields.Select(p => p.Name.ToLower()));
            var parameters = string.Join(", ", fields.Select(p => "@" + p.Name.ToLower()));

            // query building
            string updates = string.Join(", ",
                _edited.Select(k => $"{k} = VALUES({k})")
            );

            string query = $@"
            INSERT INTO {TableName} ({columns})
            VALUES ({parameters})";

            if (!string.IsNullOrWhiteSpace(updates))
                query += $"\nON DUPLICATE KEY UPDATE {updates}";

            // parameterek
            using var cmd = await Database.GetCommandAsync(query);

            foreach (var prop in fields)
            {
                cmd.Parameters.AddWithValue("@" + prop.Name.ToLower(), prop.GetValue(this));
            }

            await cmd.ExecuteNonQueryAsync();

            // auto_increment fetchelese
            var currentValue = autoField?.GetValue(this);

            if (autoField != null &&
                (currentValue == null || Convert.ToInt64(currentValue) == 0))
            {
                using var idCmd = cmd.Connection!.CreateCommand();
                idCmd.CommandText = "SELECT LAST_INSERT_ID();";

                var newId = await idCmd.ExecuteScalarAsync();
                autoField.SetValue(this, Convert.ChangeType(newId, autoField.PropertyType));
            }

            _edited.Clear();
        }
    }
}