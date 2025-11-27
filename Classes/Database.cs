using MySqlConnector;
using System.IO;

namespace StoreManager.Classes
{
    internal static class Database
    {
        //private const string InitFolderPath = "sql";
        private const string ConnectionString = "Server=db-par-02.apollopanel.com;User ID=u208217_ctR3Bmxw2K;Password=wLj2Z^PbrDnW9uUN^1atRIHV;Database=s208217_barbs;ConnectionTimeout=30";
        public static async Task Initialize()
        {
            await Task.Delay(1000);
            //    var files = await Task.Run(() => Directory.GetFiles(InitFolderPath));
            //    Array.Sort(files);

            //    foreach (var file in files)
            //    {
            //        var file_name = Path.GetFileName(file).Split(".")[0];
            //        if (!file_name.StartsWith("00"))
            //        {
            //            Console.WriteLine(file_name);
            //            var table_name = file_name.Split("_")[1];
            //            try
            //            {
            //                using var cmd = await GetCommandAsync($"SELECT * FROM {table_name} LIMIT 1");
            //                var result = await cmd.ExecuteScalarAsync();

            //                if (result is not null)
            //                    continue;
            //            } catch (Exception ex)
            //            {
            //                Console.WriteLine(ex.ToString());
            //            }
            //        }
            //        await RunFileAsync(file);
            //        Console.WriteLine($"Ran {file} without issue.");
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
