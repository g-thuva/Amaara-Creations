namespace be.DTOs.Cart
{
    public class CartItemResponse
    {
        public int Id { get; set; }
        public string ItemType { get; set; } = "Product";
        public int? ProductId { get; set; }
        public int? CustomDesignId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public string ProductImageUrl { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Subtotal { get; set; } // ProductPrice * Quantity
        public bool IsOutOfStock { get; set; }
        public int ProductStock { get; set; }
        public string? DesignName { get; set; }
        public decimal? Width { get; set; }
        public decimal? Height { get; set; }
        public string? MeasurementUnit { get; set; }
        public string? Shape { get; set; }
        public string? Material { get; set; }
        public string? Finish { get; set; }
        public string? CustomText { get; set; }
        public string? EditUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

