namespace be.DTOs.Wishlist
{
    public class WishlistItemResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal ProductPrice { get; set; }
        public string ProductImageUrl { get; set; } = string.Empty;
        public string ProductCategory { get; set; } = string.Empty;
        public bool IsOutOfStock { get; set; }
        public int ProductStock { get; set; }
        public DateTime AddedAt { get; set; }
    }
}

