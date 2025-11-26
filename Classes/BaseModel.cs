namespace StoreManager.Classes
{
    public abstract class BaseModel
    {
        protected readonly List<string> _edited = [];

        public int Id { get; protected set; } = 0;

        public BaseModel() { }
        
        protected readonly static string TableName = "base";
        protected static string ValidateInput(string input, int length)
        {
            input = input.Trim();
            if (input.Length > length)
                throw new ArgumentException("Too long");
            return input;
        }

        public async Task Save()
        {
            if (_edited.Count == 0 && Id != 0)
            {
                return;
            }
            var columns = string.Join(", ", this.GetType().GetProperties().Select(k => k.Name.ToLower()));
            var parameters = string.Join(", ", this.GetType().GetProperties().Select(k => "@" + k.Name.ToLower()));

            string updates = string.Join(", ", _edited.Select(k => $"{k} = VALUES({k})"));

            string query = $@"
            INSERT INTO {TableName} ({columns})
            VALUES ({parameters})";
            if (!string.IsNullOrWhiteSpace(updates))
                query += $"\nON DUPLICATE KEY UPDATE {updates}";

            query += ";";

            using var cmd = await Database.GetCommandAsync(query);
            foreach (var prop in this.GetType().GetProperties())
            {
                cmd.Parameters.AddWithValue("@" + prop.Name.ToLower(), prop.GetValue(this));
            }
            await cmd.ExecuteNonQueryAsync();

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
