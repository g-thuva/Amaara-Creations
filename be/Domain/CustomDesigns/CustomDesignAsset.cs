using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.CustomDesigns;
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
