using be.Domain.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Payments;
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

    [Column(TypeName = "decimal(18,2)")]
    public decimal AuthorizedAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SettledAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal RefundedAmount { get; set; }

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

    // Existing rows retain null. New attempts record the payment environment explicitly.
    [StringLength(16)]
    public string? ProviderMode { get; set; }

    [StringLength(160)]
    public string? ProviderGatewayId { get; set; }

    [StringLength(2048)]
    public string? HostedPaymentUrl { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? LastReconciledAt { get; set; }
    public DateTime? TerminalAt { get; set; }

    [StringLength(40)]
    public string? ProviderStatus { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
