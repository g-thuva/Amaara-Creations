using be.Domain.Identity;
using be.Domain.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Payments;
public class Refund
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
    public Guid IdempotencyKey { get; set; }

    [StringLength(40)]
    public string ProviderReference { get; set; } = string.Empty;
    public DateTime? LastReconciledAt { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
