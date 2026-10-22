using be.Domain.CustomDesigns;
using System.ComponentModel.DataAnnotations;

namespace be.Domain.Identity;
public enum CustomerRequestKind { CustomQuote, PrivacyDeletion }
public enum CustomerRequestStatus { Pending, InReview, Responded, Closed, Cancelled }
public sealed class CustomerRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = string.Empty;
    public User User { get; set; } = null!;
    public CustomerRequestKind Kind { get; set; }
    public CustomerRequestStatus Status { get; set; }
    [MaxLength(160)] public string Subject { get; set; } = string.Empty;
    [MaxLength(2000)] public string Details { get; set; } = string.Empty;
    public int? CustomDesignId { get; set; }
    public CustomDesign? CustomDesign { get; set; }
    public int? Quantity { get; set; }
    [MaxLength(2000)] public string? Reply { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    [Timestamp] public byte[] RowVersion { get; set; } = [];
}
