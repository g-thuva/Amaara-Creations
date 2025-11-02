namespace be.DTOs.Wishlist
{
    public class WishlistResponse
    {
        public List<WishlistItemResponse> Items { get; set; } = new List<WishlistItemResponse>();
        public int TotalItems { get; set; }
    }
}

