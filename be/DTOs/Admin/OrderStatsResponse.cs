namespace be.DTOs.Admin
{
    public class OrderStatsResponse
    {
        public int TotalOrders { get; set; }
        public int OrdersToday { get; set; }
        public int OrdersThisWeek { get; set; }
        public int OrdersThisMonth { get; set; }
        public int OrdersThisYear { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }
        public int CancelledOrders { get; set; }
    }
}

