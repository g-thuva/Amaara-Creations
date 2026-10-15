using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public class InventoryReservation
    {
        [Key]
        public long Id { get; set; }

        public int ProductId { get; set; }

        public int? ProductVariantId { get; set; }

        public int OrderId { get; set; }

        public int Quantity { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Reserved"; // Reserved, Consumed, Released

        public DateTime ReservedAt { get; set; } = DateTime.UtcNow;

        public DateTime ExpiresAt { get; set; }

        public DateTime? ConsumedAt { get; set; }

        public DateTime? ReleasedAt { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("ProductVariantId")]
        public ProductVariant? ProductVariant { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
    }

    public partial class Refund
    {
        [Key]
        public long Id { get; set; }

        public long PaymentId { get; set; }

        public int OrderId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [StringLength(3)]
        public string Currency { get; set; } = "LKR";

        [Required]
        [StringLength(500)]
        public string Reason { get; set; } = string.Empty;

        [StringLength(160)]
        public string? ProviderTransactionId { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Pending"; // Pending, Completed, Failed

        public string? RequestedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("PaymentId")]
        public Payment? Payment { get; set; }

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [ForeignKey("RequestedByUserId")]
        public User? RequestedByUser { get; set; }
    }

    public partial class CheckoutAttempt
    {
        [Key]
        public Guid Id { get; set; } // Idempotency key

        public int OrderId { get; set; }

        [Required]
        [StringLength(80)]
        public string Provider { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("OrderId")]
        public Order? Order { get; set; }
    }
}
