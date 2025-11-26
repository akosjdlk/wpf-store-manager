using MySqlConnector;
using System.IO;

namespace StoreManager.Classes
{
    internal static class Database
    {
        private const string InitFolderPath = "sql";
        private const string ConnectionString = "Server=db-par-02.apollopanel.com;User ID=u208217_ctR3Bmxw2K;Password=wLj2Z^PbrDnW9uUN^1atRIHV;Database=s208217_barbs";
        public static async Task Initialize()  // TODO: check if table exists before running 01 and 02
        {       
            var files = await Task.Run(() => Directory.GetFiles(InitFolderPath));
            Array.Sort(files);

            foreach (var file in files)
            {
                await RunFileAsync(file);
            }
        }

        public static async Task RunFileAsync(string filePath)
        {
            using var sr = new StreamReader(filePath);
            string content = sr.ReadToEnd();
            using var cmd = await GetCommandAsync(content);
            await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<MySqlCommand> GetCommandAsync(string? query = null)
        {
            var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            var cmd = conn.CreateCommand();
            if (query != null) {
                cmd.CommandText = query;
            }
            return cmd;
        }

    }
}
