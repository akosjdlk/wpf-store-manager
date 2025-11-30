using System.Windows.Controls.Primitives;

namespace StoreManager.Classes
{
    public class Product : BaseModel<Product>
    {
        protected override string TableName => "products";


        [DbField("id"), AutoIncrement]
        public int Id { get; private set; }


        [DbField("name")]
        public string Name
        {
            get => _name;
            set => Set(ref _name, ValidateInput(value, 255));
        }
        private string _name = string.Empty;

        [DbField("unit")]
        public string Unit
        {
            get => _unit;
            set => Set(ref _unit, ValidateInput(value, 50));
        }
        private string _unit = string.Empty;

        [DbField("supplier_price")]
        public decimal SupplierPrice
        {
            get => _supplierPrice;
            set => Set(ref _supplierPrice, value);
        }
        private decimal _supplierPrice;

        [DbField("sale_price")]
        public decimal SalePrice
        {
            get => _salePrice;
            set => Set(ref _salePrice, value);
        }
        private decimal _salePrice;

        [DbField("vat_percentage")]
        public int VatPercentage
        {
            get => _vatPercentage;
            set => Set(ref _vatPercentage, value);
        }
        private int _vatPercentage;

        [DbField("stock")]
        public decimal Stock
        {
            get => _stock;
            set => Set(ref _stock, value);
        }
        private decimal _stock;

        [DbField("fractionable")]
        public bool Fractionable
        {
            get => _fractionable;
            set => Set(ref _fractionable, value);
        }
        private bool _fractionable;

        public DateTime LastModified { get; private set; }

        // Utils
        public async Task<Barcode> AddBarcode(string ean)
        {
            Barcode barcode = new() { Code=ean, ProductId=Id };
            await barcode.Save();
            return barcode;
        }

		public static async Task<List<Product>> GetAll()
        {
            using var cmd = await Database.GetCommandAsync("SELECT * FROM products");
			using var reader = await cmd.ExecuteReaderAsync();
			return await FromReaderAsync(reader);
		}

        public async Task<List<Barcode>> GetBarcodes()
        {

            using var cmd = await Database.GetCommandAsync("SELECT * FROM barcodes WHERE product_id = @product_id");
            cmd.Parameters.AddWithValue("@product_id", Id);

            using var reader = await cmd.ExecuteReaderAsync();
            return await Barcode.FromReaderAsync(reader);
        }
    }
}
