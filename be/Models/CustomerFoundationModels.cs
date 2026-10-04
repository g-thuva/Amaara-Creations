using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public class Address
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string Label { get; set; } = "Default";

        [Required]
        [StringLength(120)]
        public string RecipientName { get; set; } = string.Empty;

        [StringLength(40)]
        public string? Phone { get; set; }

        [Required]
        [StringLength(200)]
        public string AddressLine1 { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AddressLine2 { get; set; }

        [Required]
        [StringLength(100)]
        public string City { get; set; } = string.Empty;

        [StringLength(100)]
        public string? DistrictOrProvince { get; set; }

        [StringLength(30)]
        public string? PostalCode { get; set; }

        [Required]
        [StringLength(80)]
        public string Country { get; set; } = "Sri Lanka";

        public bool IsDefault { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? User { get; set; }
    }

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
}
