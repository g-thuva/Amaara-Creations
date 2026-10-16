using be.Domain.Payments;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Orders;
public class CheckoutAttempt
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

    [StringLength(64)]
    public string RequestFingerprint { get; set; } = string.Empty;
    public long? PaymentId { get; set; }
    public Payment? Payment { get; set; }
}
