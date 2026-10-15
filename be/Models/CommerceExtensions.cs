using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models;

public partial class Order
{
    [StringLength(120)] public string? ShippingRecipientName { get; set; }
    [StringLength(40)] public string? ShippingPhone { get; set; }
    [StringLength(100)] public string? ShippingRegion { get; set; }
    public int? EstimatedMinimumDays { get; set; }
    public int? EstimatedMaximumDays { get; set; }
    public bool IsPickup { get; set; }
    public string? PickupLocationSnapshot { get; set; }
    [Column(TypeName = "decimal(9,4)")] public decimal TaxRateSnapshot { get; set; }
    public bool PricesIncludeTaxSnapshot { get; set; }
    public DateTime? FinalizedAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public partial class OrderItem
{
    // Historical source identity, deliberately not a foreign key to mutable cart data.
    public int? SourceCartItemId { get; set; }
    public DateTime? SourceCartUpdatedAt { get; set; }
}

public partial class Payment
{
    // Existing rows retain null. New attempts record the payment environment explicitly.
    [StringLength(16)] public string? ProviderMode { get; set; }
    [StringLength(160)] public string? ProviderGatewayId { get; set; }
    [StringLength(2048)] public string? HostedPaymentUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastReconciledAt { get; set; }
    public DateTime? TerminalAt { get; set; }
    [StringLength(40)] public string? ProviderStatus { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public partial class CheckoutAttempt
{
    [StringLength(64)] public string RequestFingerprint { get; set; } = string.Empty;
    public long? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}

public partial class Refund
{
    public Guid IdempotencyKey { get; set; }
    [StringLength(40)] public string ProviderReference { get; set; } = string.Empty;
    public DateTime? LastReconciledAt { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public partial class ShippingZone
{
    [StringLength(80)] public string Country { get; set; } = string.Empty;
    [StringLength(100)] public string? Region { get; set; }
    [StringLength(30)] public string? PostalCodePrefix { get; set; }
    public bool RequiresRegion { get; set; }
    public bool RequiresPostalCode { get; set; }
    public bool RequiresPhone { get; set; } = true;
}

public partial class ShippingMethod
{
    [StringLength(50)] public string Code { get; set; } = string.Empty;
    [StringLength(3)] public string Currency { get; set; } = "LKR";
    [Column(TypeName = "decimal(18,2)")] public decimal? MinimumOrderValue { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal? MaximumOrderValue { get; set; }
    public int? EstimatedMinimumDays { get; set; }
    public int? EstimatedMaximumDays { get; set; }
    public bool IsPickup { get; set; }
    [StringLength(500)] public string? PickupLocation { get; set; }
    public bool IsDevelopmentValue { get; set; }
    public int DisplayOrder { get; set; }
}

public partial class Shipment
{
    [StringLength(500)] public string? TrackingUrl { get; set; }
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public partial class Promotion
{
    [StringLength(3)] public string Currency { get; set; } = "LKR";
    [Column(TypeName = "decimal(18,2)")] public decimal? MaximumDiscount { get; set; }
    public int? PerCustomerUsageLimit { get; set; }
    public bool AppliesToStandard { get; set; } = true;
    public bool AppliesToCustom { get; set; } = true;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}

public class PromotionRedemption
{
    public long Id { get; set; }
    public int PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    [StringLength(450)] public string UserId { get; set; } = string.Empty;
    [StringLength(20)] public string Status { get; set; } = "Reserved";
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public class PaymentWebhookEvent
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public Payment Payment { get; set; } = null!;
    [StringLength(64)] public string PayloadHash { get; set; } = string.Empty;
    [StringLength(40)] public string ProviderStatus { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public class CommerceNotification
{
    public long Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;
    [StringLength(120)] public string EventKey { get; set; } = string.Empty;
    [StringLength(120)] public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
}
