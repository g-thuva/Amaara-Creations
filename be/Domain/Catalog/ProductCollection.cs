namespace be.Domain.Catalog;
public class ProductCollection
{
    public int ProductId { get; set; }
    public int CollectionId { get; set; }
    public int SortOrder { get; set; }
    public Product? Product { get; set; }
    public Collection? Collection { get; set; }
}
