namespace be.Application.Features.Customers
{
    public class CustomerStatsResponse
    {
        public string Currency { get; set; } = "LKR";
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public int TotalCartItems { get; set; }
        public int TotalWishlistItems { get; set; }
        public int TotalReviews { get; set; }
        public DateTime? LastOrderDate { get; set; }
    }
}
