using be.Domain.Common;
namespace be.Application.Features.Orders
{
    public class OrderResponse
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string UserId { get; set; } = string.Empty;
        public string UserName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public decimal Total { get; set; }
        public decimal Subtotal { get; set; }
        public decimal DiscountTotal { get; set; }
        public decimal ShippingTotal { get; set; }
        public decimal TaxTotal { get; set; }
        public decimal PaymentFee { get; set; }
        public string Currency { get; set; } = Money.DefaultCurrency;
        public bool PricesIncludeTax { get; set; }
        public string PaymentStatus { get; set; } = "Unpaid";
        public string FulfilmentStatus { get; set; } = "NotReady";
        public string? ShippingMethodName { get; set; }
        public string? ShippingRecipientName { get; set; }
        public string? ShippingPhone { get; set; }
        public string? ShippingRegion { get; set; }
        public string? CouponCode { get; set; }
        public bool IsPickup { get; set; }
        public string? PickupLocation { get; set; }
        public List<PaymentSummaryDto> Payments { get; set; } = [];
        public List<ShipmentSummaryDto> Shipments { get; set; } = [];
        public List<OrderHistoryDto> History { get; set; } = [];
        public List<string> AvailableTransitions { get; set; } = [];
        public string Status { get; set; } = string.Empty; // Pending, Processing, Shipped, Delivered, Cancelled
        public string? ShippingAddress { get; set; }
        public string? ShippingCity { get; set; }
        public string? ShippingPostalCode { get; set; }
        public string? ShippingCountry { get; set; }
        public string? Notes { get; set; }
        public List<OrderItemResponse> OrderItems { get; set; } = new List<OrderItemResponse>();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public record PaymentSummaryDto(
        long Id,
        string Status,
        decimal Amount,
        string Currency,
        string Provider,
        string? ProviderReference,
        string? ProviderTransactionId,
        DateTime? PaidAt);
    public record ShipmentSummaryDto(
        long Id,
        string Status,
        string? Courier,
        string? TrackingNumber,
        string? TrackingUrl,
        DateTime? ShippedAt,
        DateTime? DeliveredAt);
    public record OrderHistoryDto(string? PreviousStatus, string NewStatus, string? Reason, DateTime CreatedAt);
}
