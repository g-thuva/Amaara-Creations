using be.Domain.Orders;
using System.ComponentModel.DataAnnotations;

namespace be.Domain.Notifications;
public class CommerceNotification
{
    public long Id { get; set; }
    public int OrderId { get; set; }
    public Order Order { get; set; } = null !;

    [StringLength(120)]
    public string EventKey { get; set; } = string.Empty;

    [StringLength(120)]
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? SentAt { get; set; }
    public int Attempts { get; set; }
    public DateTime? LastAttemptAt { get; set; }
}
