using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace StoreManager.Classes
{
	[AttributeUsage(AttributeTargets.Property, Inherited = true)]
	public class DbFieldAttribute(string columnName) : Attribute
	{
		public string ColumnName { get; } = columnName;
	}

	[AttributeUsage(AttributeTargets.Property)]
	public class AutoIncrementAttribute : Attribute { }


	public abstract class BaseModel<TSelf> where TSelf : BaseModel<TSelf>, new()
	{
		protected readonly List<string> _edited = [];

		protected abstract string TableName { get; }

		private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _dbFieldsCache = new();
		private static readonly ConcurrentDictionary<Type, PropertyInfo?> _autoFieldCache = new();

		protected PropertyInfo[] GetDbFields()
		{
			var fields = _dbFieldsCache.GetOrAdd(typeof(TSelf), t =>
				[.. t.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetCustomAttribute<DbFieldAttribute>() != null)]
			);
			return fields;
		}

		private static PropertyInfo? GetAutoIncrementField()
		{
			return _autoFieldCache.GetOrAdd(typeof(TSelf), t =>
				t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				 .FirstOrDefault(p => p.GetCustomAttribute<AutoIncrementAttribute>() != null)
			);
		}

		public static async Task<List<TSelf>> FromReaderAsync(DbDataReader reader)
		{
			var list = new List<TSelf>();
			var fields = new TSelf().GetDbFields();

			var map = fields.ToDictionary(
				p => p.GetCustomAttribute<DbFieldAttribute>()!.ColumnName,
				p => p
			);

			while (await reader.ReadAsync())
			{
				var obj = new TSelf();

				for (int i = 0; i < reader.FieldCount; i++)
				{
					var col = reader.GetName(i);
					if (!map.TryGetValue(col, out var prop))
						continue;


					var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
					prop.SetValue(obj, value);
				}

				list.Add(obj);
			}

			return list;
		}

		public static async Task<TSelf?> FromFirstRowAsync(DbDataReader reader)
		{
			if (!await reader.ReadAsync())
			{
				await reader.CloseAsync();
				return null;
			}

			var obj = new TSelf();
			var fields = obj.GetDbFields();

			var map = fields.ToDictionary(
				p => p.GetCustomAttribute<DbFieldAttribute>()!.ColumnName.ToLower(),
				p => p
			);

			for (int i = 0; i < reader.FieldCount; i++)
			{
				var col = reader.GetName(i).ToLower();
				if (!map.TryGetValue(col, out var prop))
					continue;

				var value = await reader.IsDBNullAsync(i) ? null : reader.GetValue(i);
				prop.SetValue(obj, value);
			}
			await reader.CloseAsync();
            return obj;
		}

		protected void Set<T>(ref T field, T value, [CallerMemberName] string propName = "")
		{
			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
				field = value;
				_edited.Add(propName.ToLower());
			}
		}

		protected static string ValidateInput(string input, int length)
		{
			input = input.Trim();
			if (input.Length > length)
				throw new ArgumentException("Too long");
			return input;
		}

		public async Task Save()
		{
			var autoField = BaseModel<TSelf>.GetAutoIncrementField();
			var fields = GetDbFields().Where(p => p != autoField);

			var columns = string.Join(", ",
				fields.Select(p => p.GetCustomAttribute<DbFieldAttribute>()!.ColumnName)
			);

			var parameters = string.Join(", ",
				fields.Select(p => "@" + p.GetCustomAttribute<DbFieldAttribute>()!.ColumnName)
			);

			var update = string.Join(", ",
				_edited.Select(k =>
				{
					var prop = GetDbFields().First(p => p.Name.Equals(k));
					var col = prop.GetCustomAttribute<DbFieldAttribute>()!.ColumnName;
					return $"{col} = {col}";
				})
			);

			var sql = $"INSERT INTO {TableName} ({columns}) VALUES ({parameters})";
			if (update.Length > 0)
				sql += $" ON DUPLICATE KEY UPDATE {update}";

			using var cmd = await Database.GetCommandAsync(sql);

			foreach (var p in fields)
			{
				var col = p.GetCustomAttribute<DbFieldAttribute>()!.ColumnName;
				cmd.Parameters.AddWithValue("@" + col, p.GetValue(this));
			}

			await cmd.ExecuteNonQueryAsync();

			if (autoField != null &&
				(autoField.GetValue(this) == null ||
				 Convert.ToInt64(autoField.GetValue(this)) == 0))
			{
				using var idCmd = cmd.Connection!.CreateCommand();
				idCmd.CommandText = "SELECT LAST_INSERT_ID()";
				var id = await idCmd.ExecuteScalarAsync();
				autoField.SetValue(this, Convert.ChangeType(id, autoField.PropertyType));
			}

			_edited.Clear();
		}
		public override string ToString() {
			return $"{typeof(TSelf).Name.Title()}({string.Join(", ", GetDbFields().Select(f => $"{f.Name}={f.GetValue(this)}"))})";
		}
	}
}
