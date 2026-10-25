using be.Domain.Common;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.CustomDesigns;
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
    public string Currency { get; set; } = Money.DefaultCurrency;
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
