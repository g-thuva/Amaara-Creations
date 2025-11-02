namespace be.DTOs.Admin
{
    public class ProductStatsResponse
    {
        public int TotalProducts { get; set; }
        public int ActiveProducts { get; set; }
        public int InactiveProducts { get; set; }
        public int OutOfStockProducts { get; set; }
        public int LowStockProducts { get; set; } // Stock < 10
        public List<TopSellingProduct> TopSellingProducts { get; set; } = new List<TopSellingProduct>();
    }

    public class TopSellingProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImageUrl { get; set; } = string.Empty;
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}

