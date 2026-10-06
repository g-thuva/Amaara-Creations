using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public enum CustomDesignStatus
    {
        Draft = 0,
        Ready = 1,
        InCart = 2,
        Ordered = 3,
        Archived = 4
    }

    public class CustomDesign
    {
        [Key]
        public int Id { get; set; }

        public string? UserId { get; set; }

        [Required]
        [StringLength(160)]
        public string Name { get; set; } = "Untitled custom sticker";

        public int DesignSchemaVersion { get; set; } = 1;

        public int? BuilderConfigurationVersionId { get; set; }

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

        [StringLength(120)]
        public string? ShapeCode { get; set; }

        [StringLength(120)]
        public string? FontCode { get; set; }

        [StringLength(120)]
        public string? ColourCode { get; set; }

        [StringLength(160)]
        public string? CustomText { get; set; }

        [StringLength(20)]
        public string TextAlignment { get; set; } = "center";

        public int? PreviewMediaId { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? CalculatedPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? UnitPrice { get; set; }

        [StringLength(80)]
        public string? PricingRuleVersion { get; set; }

        public DateTime? QuotedAt { get; set; }

        public ProofStatus ProofStatus { get; set; } = ProofStatus.Preparing;

        public ProductionStatus ProductionStatus { get; set; } = ProductionStatus.NotStarted;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [ForeignKey("UserId")]
        public User? User { get; set; }

        [ForeignKey("PreviewMediaId")]
        public CustomDesignAsset? PreviewMedia { get; set; }

        [ForeignKey("BuilderConfigurationVersionId")]
        public CustomBuilderConfigurationVersion? BuilderConfigurationVersion { get; set; }

        public ICollection<CustomDesignAsset> Assets { get; set; } = new List<CustomDesignAsset>();

        public ICollection<CustomDesignProofRevision> ProofRevisions { get; set; } = new List<CustomDesignProofRevision>();
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

        public int? Width { get; set; }

        public int? Height { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CustomDesignId")]
        public CustomDesign? CustomDesign { get; set; }
    }
}
