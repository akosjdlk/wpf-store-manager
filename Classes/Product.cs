using StoreManager.Classes;

namespace StoreManager.Models
{
    public class Product : BaseModel
    {
        protected override string TableName => "products";

        private string _name = "";
        private decimal _price;
        private int _stock;

        [DbField]
        public string Name
        {
            get => _name;
            set => Set(ref _name, ValidateInput(value, 100));
        }

        [DbField]
        public decimal Price
        {
            get => _price;
            set => Set(ref _price, value);
        }

        [DbField]
        public int Stock
        {
            get => _stock;
            set => Set(ref _stock, value);
        }

        // Parameterless constructor is required
        public Product() { }

        public Product(string name, decimal price, int stock)
        {
            _name = ValidateInput(name, 100);
            _price = price;
            _stock = stock;
        }
    }
}
