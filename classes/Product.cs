class Product {

    private int myVar;
    public int MyProperty
    {
        get { return myVar; }
        set { myVar = value; }
    }
    
}

class ProductIdentifier
{
    public readonly int id;  // DB pk
    public readonly int ean;  // barcode

    
}