using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public class AuditLog
    {
        [Key]
        public long Id { get; set; }

        public string? ActorUserId { get; set; }

        [Required]
        [StringLength(120)]
        public string Action { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string EntityType { get; set; } = string.Empty;

        [StringLength(120)]
        public string? EntityId { get; set; }

        public string? OldValuesJson { get; set; }

        public string? NewValuesJson { get; set; }

        [StringLength(100)]
        public string? CorrelationId { get; set; }

        [StringLength(64)]
        public string? IpAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ActorUserId")]
        public User? ActorUser { get; set; }
    }
}
