using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Identity;
public class RefreshSession
{
    [Key]
    public long Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    [StringLength(128)]
    public string TokenHash { get; set; } = string.Empty;

    [Required]
    [StringLength(64)]
    public string FamilyId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public long? ReplacedBySessionId { get; set; }

    [StringLength(300)]
    public string? UserAgent { get; set; }

    [StringLength(64)]
    public string? IpAddress { get; set; }
    public DateTime? LastUsedAt { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("ReplacedBySessionId")]
    public RefreshSession? ReplacedBySession { get; set; }
}
