namespace be.Application.Features.Catalog
{
    public class ProductListResponse
    {
        public List<ProductResponse> Products { get; set; } = new();
        public string Currency { get; set; } = "LKR";
        public int TotalCount { get; set; }
        public int PublishedCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}
