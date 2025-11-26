namespace StoreManager.Classes
{

    public class Product : BaseModel
    {
        protected override string TableName => "products";
        [DbField]
        public int ProductId { get; set; }
        
    }

}