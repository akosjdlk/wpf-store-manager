namespace StoreManager.Classes
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DbFieldAttribute : Attribute { }
    public abstract class BaseModel
    {
        protected readonly List<string> _edited = [];

        [DbField]
        public int Id { get; protected set; } = 0;

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
        public async Task Save()
        {
            var fields = GetDbFields();

            var columns = string.Join(", ", fields.Select(p => p.Name.ToLower()));
            var parameters = string.Join(", ", fields.Select(p => "@" + p.Name.ToLower()));

            string updates = string.Join(", ",
                _edited.Select(k => $"{k} = VALUES({k})")
            );

            string query = $@"
            INSERT INTO {TableName} ({columns})
            VALUES ({parameters})";

            if (!string.IsNullOrWhiteSpace(updates))
                query += $"\nON DUPLICATE KEY UPDATE {updates}";

            using var cmd = await Database.GetCommandAsync(query);

            foreach (var prop in fields)
            {
                cmd.Parameters.AddWithValue("@" + prop.Name.ToLower(), prop.GetValue(this));
            }

            await cmd.ExecuteNonQueryAsync();

            // Fetch ID for new rows
            if (Id == 0)
            {
                using var idCmd = cmd.Connection!.CreateCommand();
                idCmd.CommandText = "SELECT LAST_INSERT_ID();";
                Id = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
            }

            _edited.Clear();
        }
    }
}