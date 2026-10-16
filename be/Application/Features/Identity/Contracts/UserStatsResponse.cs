namespace be.Application.Features.Identity
{
    public class UserStatsResponse
    {
        public int TotalOrders { get; set; }
        public int TotalWishlistItems { get; set; }
        public int TotalCartItems { get; set; }
        public int TotalReviews { get; set; }
        public decimal TotalSpent { get; set; }
    }
}
