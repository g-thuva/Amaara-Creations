using System.ComponentModel.DataAnnotations;

namespace be.Domain.Payments;
public class PaymentWebhookEvent
{
    public long Id { get; set; }
    public long PaymentId { get; set; }
    public Payment Payment { get; set; } = null !;

    [StringLength(64)]
    public string PayloadHash { get; set; } = string.Empty;

    [StringLength(40)]
    public string ProviderStatus { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}
