using MySqlConnector;
using System.IO;

namespace StoreManager.Classes
{
    internal static class Database
    {
        private const string ConnectionString = "Server=db-par-02.apollopanel.com;User ID=u208217_G0LNIAe1KA;Password=sd5JqlpVljm+EOT=IEzta6np;Database=s208217_akos;ConnectionTimeout=30";

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
