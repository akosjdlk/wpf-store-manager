using System.Windows.Documents;

namespace StoreManager.Classes
{
    public class Barcode : BaseModel<Barcode>
    {
        protected override string TableName => "barcodes";

        [DbField("code")]
        public string Code
        {
            get => _code;
            set => Set(ref _code, ValidateCode(value));
        }
        private string _code = string.Empty;

        [DbField("product_id")]
        public int ProductId
        {
            get => _productId;
            set => Set(ref _productId, value);
        }
        private int _productId;

        private static string ValidateCode(string code)
        {
            if (code is null)
            {
                throw new ArgumentException("Barcode cannot be null");
            }
            code = code.Trim();

            if (code.Length == 0)
                throw new ArgumentException("Barcode cannot be empty");

            if (code.Length > 13)
                throw new ArgumentException("Barcode cannot exceed 13 characters");

            return code;
        }

        public static async Task<List<Barcode>> GetAll()
        {
            using var cmd = await Database.GetCommandAsync("SELECT * FROM barcodes");
            return await FromReaderAsync(await cmd.ExecuteReaderAsync());
        }
    }
}