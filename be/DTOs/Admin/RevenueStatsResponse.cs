namespace be.DTOs.Admin
{
    public class RevenueStatsResponse
    {
        public decimal TotalRevenue { get; set; }
        public decimal RevenueToday { get; set; }
        public decimal RevenueThisWeek { get; set; }
        public decimal RevenueThisMonth { get; set; }
        public decimal RevenueThisYear { get; set; }
        public List<MonthlyRevenue> MonthlyRevenue { get; set; } = new List<MonthlyRevenue>();
    }

    public class MonthlyRevenue
    {
        public string Month { get; set; } = string.Empty;
        public int Year { get; set; }
        public decimal Revenue { get; set; }
    }
}

