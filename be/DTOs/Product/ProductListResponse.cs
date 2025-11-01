namespace be.DTOs.Product
{
    public class ProductListResponse
    {
        public List<ProductResponse> Products { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }
}

