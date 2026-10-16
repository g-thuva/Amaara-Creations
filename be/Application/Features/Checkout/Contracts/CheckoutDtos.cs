using be.Application.Features.Promotions;
using be.Application.Features.Shipping;
using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Checkout;
public class CheckoutQuoteRequest
{
    [Range(1, int.MaxValue)]
    public int AddressId { get; set; }
    public int? ShippingMethodId { get; set; }

    [StringLength(40)]
    public string? CouponCode { get; set; }
}

public class CheckoutSessionRequest : CheckoutQuoteRequest
{
    public Guid IdempotencyKey { get; set; }

    [Required, StringLength(64)]
    public string QuoteVersion { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Notes { get; set; }
}

public record CheckoutItemDto(
        int CartItemId,
        string ItemType,
        string Name,
        int Quantity,
        decimal UnitPrice,
        decimal Subtotal,
        string? ConfigurationSnapshotJson);
public record CheckoutQuoteDto(
        IReadOnlyList<CheckoutItemDto> Items,
        decimal Subtotal,
        decimal Discount,
        decimal Shipping,
        decimal Tax,
        decimal PaymentFee,
        decimal Total,
        string Currency,
        IReadOnlyList<ShippingOptionDto> AvailableShippingMethods,
        CouponResultDto? CouponResult,
        string QuoteVersion,
        DateTime ExpiresAt,
        bool PaymentAvailable,
        bool CanPay,
        bool PricesIncludeTax = false,
        string? PaymentProvider = null,
        string? PaymentMode = null);
