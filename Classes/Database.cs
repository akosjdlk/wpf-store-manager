using MySqlConnector;
using System.IO;

namespace StoreManager.Classes
{
    internal static class Database
    {
        private const string InitFolderPath = "sql";
        private const string ConnectionString = "Server=db-par-02.apollopanel.com;User ID=u208217_ctR3Bmxw2K;Password=wLj2Z^PbrDnW9uUN^1atRIHV;Database=s208217_barbs;ConnectionTimeout=30";
        public static async Task Initialize()  // TODO: check if table exists before running 01 and 02
        {       
            var files = await Task.Run(() => Directory.GetFiles(InitFolderPath));
            Array.Sort(files);

            foreach (var file in files)
            {
                Console.WriteLine(file);
                if (!file.Split("\\")[1].StartsWith("00"))
                {
                    try
                    {

                        using var cmd = await GetCommandAsync("SELECT * FROM products LIMIT 1");
                        var result = await cmd.ExecuteScalarAsync();

                        if (result is not null)
                        {
                            return;
                        }
                    } catch
                    {

                    }
                }
                await RunFileAsync(file);
                Console.WriteLine("Ran file without issue.");
            }
        }

        public static async Task RunFileAsync(string filePath)
        {
            using var sr = new StreamReader(filePath);
            string content = sr.ReadToEnd();
            using var cmd = await GetCommandAsync(content);
            //await cmd.ExecuteNonQueryAsync();
        }

        public static async Task<MySqlCommand> GetCommandAsync(string? query = null)
        {

            var conn = new MySqlConnection(ConnectionString);
            try
            {
                await conn.OpenAsync();
            }
            catch (Exception ex) {
                Console.WriteLine(ex.ToString());
            }

            var cmd = conn.CreateCommand();
            if (query != null) {
                cmd.CommandText = query;
            }
            return cmd;
        }

    }
}
