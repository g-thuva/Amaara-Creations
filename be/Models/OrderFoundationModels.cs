using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public enum OrderItemType
    {
        Product = 0,
        CustomDesign = 1
    }

    public enum PaymentStatus
    {
        Pending = 0,
        Authorized = 1,
        Paid = 2,
        Failed = 3,
        Refunded = 4,
        Cancelled = 5
    }

    public enum ShipmentStatus
    {
        Pending = 0,
        Ready = 1,
        Shipped = 2,
        Delivered = 3,
        Failed = 4,
        Cancelled = 5
    }

    public enum DiscountType
    {
        Percentage = 0,
        FixedAmount = 1
    }

    public class OrderStatusHistory
    {
        [Key]
        public long Id { get; set; }

        public int OrderId { get; set; }

        [StringLength(40)]
        public string? PreviousStatus { get; set; }

        [Required]
        [StringLength(40)]
        public string NewStatus { get; set; } = string.Empty;

        public string? ChangedByUserId { get; set; }

        [StringLength(500)]
        public string? Reason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [ForeignKey("ChangedByUserId")]
        public User? ChangedByUser { get; set; }
    }

    public class Payment
    {
        [Key]
        public long Id { get; set; }

        public int OrderId { get; set; }

        [Required]
        [StringLength(80)]
        public string Provider { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string Method { get; set; } = string.Empty;

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "LKR";

        [StringLength(160)]
        public string? ProviderTransactionId { get; set; }

        [StringLength(160)]
        public string? ProviderReference { get; set; }

        [StringLength(500)]
        public string? FailureReason { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? PaidAt { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
    }

    public class ShippingZone
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<ShippingMethod> ShippingMethods { get; set; } = new List<ShippingMethod>();
    }

    public class ShippingMethod
    {
        [Key]
        public int Id { get; set; }

        public int? ShippingZoneId { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? FreeShippingThreshold { get; set; }

        public bool IsActive { get; set; } = true;

        [ForeignKey("ShippingZoneId")]
        public ShippingZone? ShippingZone { get; set; }
    }

    public class Shipment
    {
        [Key]
        public long Id { get; set; }

        public int OrderId { get; set; }

        [StringLength(120)]
        public string? Method { get; set; }

        public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;

        [StringLength(120)]
        public string? Courier { get; set; }

        [StringLength(120)]
        public string? TrackingNumber { get; set; }

        public DateTime? ShippedAt { get; set; }

        public DateTime? DeliveredAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
    }

    public class Promotion
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(40)]
        public string Code { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;

        [Column(TypeName = "decimal(18,2)")]
        public decimal DiscountValue { get; set; }

        public DateTime StartsAt { get; set; }

        public DateTime? EndsAt { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? MinimumSpend { get; set; }

        public int? UsageLimit { get; set; }

        public int UsageCount { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
