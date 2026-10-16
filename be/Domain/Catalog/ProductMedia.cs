using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Catalog;
public class ProductMedia
{
    [Key]
    public int Id { get; set; }
    public int ProductId { get; set; }

    [Required]
    [StringLength(500)]
    public string StorageKey { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Url { get; set; }

    [Required]
    [StringLength(50)]
    public string MediaType { get; set; } = "image";

    [StringLength(200)]
    public string? AltText { get; set; }

    [StringLength(255)]
    public string? OriginalFileName { get; set; }

    [StringLength(120)]
    public string? ContentType { get; set; }
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }

    [StringLength(80)]
    public string StorageProvider { get; set; } = "local";
    public string? CreatedByUserId { get; set; }
    public int SortOrder { get; set; }
    public bool IsPrimary { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
}
