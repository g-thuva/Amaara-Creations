namespace be.DTOs.Product
{
    public class ProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public decimal BasePrice { get; set; }
        public string Description { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? BaseSku { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public bool IsOutOfStock => Stock == 0;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

