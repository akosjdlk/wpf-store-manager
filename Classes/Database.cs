using MySqlConnector;
using System.IO;
using System.Data;
using System.Data.Common;

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
        
        public static async Task<T> QueryAsync<T>(string sql, Func<DbDataReader, Task<T>> handler)
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            try
            {
                return await handler(reader);
            }
            finally
            {
                await reader.CloseAsync();
            }
        }

        public static async Task<T> QueryAsync<T>(string sql, Action<MySqlCommand> configure, Func<DbDataReader, Task<T>> handler)
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;
            configure(cmd as MySqlCommand ?? throw new ArgumentException("Expected MySqlCommand"));

            await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.CloseConnection);
            try
            {
                return await handler(reader);
            }
            finally
            {
                await reader.CloseAsync();
            }
        }

        public static async Task<T> WithCommandAsync<T>(string sql, Func<MySqlCommand, Task<T>> handler)
        {
            await using var conn = new MySqlConnection(ConnectionString);
            await conn.OpenAsync();

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = sql;

            return await handler(cmd as MySqlCommand ?? throw new ArgumentException("Expected MySqlCommand"));
        }

    }
}
