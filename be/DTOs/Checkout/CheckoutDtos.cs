using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Checkout;

public class CheckoutQuoteRequest
{
    [Range(1, int.MaxValue)] public int AddressId { get; set; }
    public int? ShippingMethodId { get; set; }
    [StringLength(40)] public string? CouponCode { get; set; }
}
public class CheckoutSessionRequest : CheckoutQuoteRequest
{
    public Guid IdempotencyKey { get; set; }
    [Required, StringLength(64)] public string QuoteVersion { get; set; } = string.Empty;
    [StringLength(500)] public string? Notes { get; set; }
}
public record ShippingOptionDto(int Id, string Code, string Name, decimal Amount, string Currency, int? EstimatedMinimumDays, int? EstimatedMaximumDays, bool IsPickup, string? PickupLocation, bool IsDevelopmentValue);
public record CheckoutItemDto(int CartItemId, string ItemType, string Name, int Quantity, decimal UnitPrice, decimal Subtotal, string? ConfigurationSnapshotJson);
public record CouponResultDto(string Code, bool Applied, string? ErrorCode, string? Message, decimal Discount);
public record CheckoutQuoteDto(IReadOnlyList<CheckoutItemDto> Items, decimal Subtotal, decimal Discount, decimal Shipping, decimal Tax, decimal PaymentFee, decimal Total, string Currency, IReadOnlyList<ShippingOptionDto> AvailableShippingMethods, CouponResultDto? CouponResult, string QuoteVersion, DateTime ExpiresAt, bool PaymentAvailable, bool CanPay, bool PricesIncludeTax = false, string? PaymentProvider = null, string? PaymentMode = null);
public record PaymentSessionDto(int OrderId, long PaymentId, string Status, string? RedirectUrl, DateTime? ExpiresAt);
public record RetryPaymentRequest(Guid IdempotencyKey);
public record RefundRequest([Range(typeof(decimal), "0.01", "9999999999999999")] decimal Amount, [Required, StringLength(500)] string Reason, Guid IdempotencyKey);
public record ShipmentRequest([Required] string Status, [StringLength(120)] string? Courier, [StringLength(120)] string? TrackingNumber, [StringLength(500)] string? TrackingUrl, [StringLength(500)] string? Notes = null);
