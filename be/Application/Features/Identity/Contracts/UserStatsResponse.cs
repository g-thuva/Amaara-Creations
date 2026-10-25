using be.Domain.Common;
namespace be.Application.Features.Identity
{
    public class UserStatsResponse
    {
        public int TotalOrders { get; set; }
        public int TotalWishlistItems { get; set; }
        public int TotalCartItems { get; set; }
        public int TotalReviews { get; set; }
        public decimal TotalSpent { get; set; }
        public string Currency { get; set; } = Money.DefaultCurrency;
        public IReadOnlyDictionary<string, decimal> OrderTotalsByCurrency { get; set; } = new Dictionary<string, decimal>();
    }
}
