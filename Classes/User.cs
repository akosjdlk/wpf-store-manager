using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StoreManager.Classes
{
    internal class User : BaseModel<User>
    {
        protected override string TableName => "users";

        [DbField("id"), AutoIncrement]
        public int Id { get; private set; }

        private int _can_access_storage;

        [DbField("can_access_storage")]
        public bool CanAccessStorage
        {
            get => Convert.ToBoolean(_can_access_storage);
            set => Set(ref _can_access_storage, Convert.ToInt16(value));
        }

        private string _password = string.Empty;

        [DbField("password")]
        public string Password
        {
            get => _password;
            private set => Set(ref _password, value);
        }

        public static async Task<User?> GetUser(int id)
        {
            using var cmd = await Database.GetCommandAsync("SELECT * FROM users WHERE id = @id");
            cmd.Parameters.AddWithValue("id", id);
            return await FromFirstRowAsync(await cmd.ExecuteReaderAsync());
        }
    } 
}
