using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Catalog;
public class ProductVariant
{
    [Key]
    public int Id { get; set; }
    public int ProductId { get; set; }

    [Required]
    [StringLength(64)]
    public string Sku { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PriceOverride { get; set; }
    public int StockQuantity { get; set; }

    [Range(0, int.MaxValue)]
    public int LowStockThreshold { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [Timestamp]
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }
}
