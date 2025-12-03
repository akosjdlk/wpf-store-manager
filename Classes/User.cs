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

        private int _canAccessStorage;

        [DbField("can_access_storage")]
        public bool CanAccessStorage
        {
            get => Convert.ToBoolean(_canAccessStorage);
            set => Set(ref _canAccessStorage, Convert.ToInt16(value));
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
            return await Database.QueryAsync("SELECT * FROM users WHERE id = @id",
                cmd => cmd.Parameters.AddWithValue("id", id),
                FromFirstRowAsync);
        }
    } 
}
