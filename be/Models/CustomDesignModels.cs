using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public enum CustomDesignStatus
    {
        Draft = 0,
        Priced = 1,
        AddedToCart = 2,
        Ordered = 3,
        Archived = 4
    }

    public class CustomDesign
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        public CustomDesignStatus Status { get; set; } = CustomDesignStatus.Draft;

        [Required]
        public string DesignJson { get; set; } = "{}";

        [Column(TypeName = "decimal(18,2)")]
        public decimal Width { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Height { get; set; }

        public int Quantity { get; set; } = 1;

        [StringLength(120)]
        public string? MaterialCode { get; set; }

        [StringLength(120)]
        public string? FinishCode { get; set; }

        public int? PreviewMediaId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CalculatedPrice { get; set; }

        [StringLength(80)]
        public string? PricingRuleVersion { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("PreviewMediaId")]
        public CustomDesignAsset? PreviewMedia { get; set; }

        public ICollection<CustomDesignAsset> Assets { get; set; } = new List<CustomDesignAsset>();
    }

    public class CustomDesignAsset
    {
        [Key]
        public int Id { get; set; }

        public int CustomDesignId { get; set; }

        [Required]
        [StringLength(500)]
        public string StorageKey { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string AssetType { get; set; } = string.Empty;

        [StringLength(255)]
        public string? OriginalFileName { get; set; }

        [StringLength(120)]
        public string? ContentType { get; set; }

        public long? SizeBytes { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CustomDesignId")]
        public CustomDesign? CustomDesign { get; set; }
    }
}
