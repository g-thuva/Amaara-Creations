using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models;

public enum BuilderVersionStatus
{
    Draft = 0,
    Published = 1,
    Superseded = 2
}

public enum BuilderOptionGroup
{
    Shape = 0,
    Material = 1,
    Finish = 2,
    Font = 3,
    Colour = 4
}

public enum ProofStatus
{
    NotRequired = 0,
    Preparing = 1,
    AwaitingApproval = 2,
    ChangesRequested = 3,
    Approved = 4
}

public enum ProofRevisionStatus
{
    AwaitingApproval = 0,
    ChangesRequested = 1,
    Approved = 2,
    Superseded = 3
}

public enum ProductionStatus
{
    NotStarted = 0,
    Queued = 1,
    Printing = 2,
    Finishing = 3,
    QualityCheck = 4,
    Ready = 5
}

public class CustomBuilderConfigurationVersion
{
    [Key]
    public int Id { get; set; }

    public int VersionNumber { get; set; }

    public BuilderVersionStatus Status { get; set; } = BuilderVersionStatus.Draft;

    [Required, StringLength(20)]
    public string MeasurementUnit { get; set; } = "cm";

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinimumWidth { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaximumWidth { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal WidthStep { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinimumHeight { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MaximumHeight { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal HeightStep { get; set; }

    public int MinimumQuantity { get; set; }

    public int MaximumQuantity { get; set; }

    public int QuantityStep { get; set; }

    [Column(TypeName = "decimal(18,4)")]
    public decimal PricePerSquareUnit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal MinimumLinePrice { get; set; }

    [Required, StringLength(3)]
    public string Currency { get; set; } = "LKR";

    public bool ArtworkRequired { get; set; }

    public bool ProofRequired { get; set; } = true;

    [StringLength(500)]
    public string? InternalNotes { get; set; }

    public DateTime? PublishedAt { get; set; }

    public string? PublishedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    public ICollection<CustomBuilderOption> Options { get; set; } = new List<CustomBuilderOption>();
}

public class CustomBuilderOption
{
    [Key]
    public int Id { get; set; }

    public int ConfigurationVersionId { get; set; }

    public BuilderOptionGroup Group { get; set; }

    [Required, StringLength(80)]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(120)]
    public string Label { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPriceAdjustment { get; set; }

    public bool IsActive { get; set; } = true;

    public int SortOrder { get; set; }

    [ForeignKey(nameof(ConfigurationVersionId))]
    public CustomBuilderConfigurationVersion? ConfigurationVersion { get; set; }
}

public class CustomDesignProofRevision
{
    [Key]
    public int Id { get; set; }

    public int CustomDesignId { get; set; }

    public int AssetId { get; set; }

    public int RevisionNumber { get; set; }

    public ProofRevisionStatus Status { get; set; } = ProofRevisionStatus.AwaitingApproval;

    [StringLength(1000)]
    public string? CustomerVisibleMessage { get; set; }

    [StringLength(1000)]
    public string? CustomerResponse { get; set; }

    public DateTime? CustomerResponseAt { get; set; }

    public string? UploadedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey(nameof(CustomDesignId))]
    public CustomDesign? CustomDesign { get; set; }

    [ForeignKey(nameof(AssetId))]
    public CustomDesignAsset? Asset { get; set; }
}
