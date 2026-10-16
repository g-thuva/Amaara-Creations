using be.Domain.Catalog;
using be.Domain.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Inventory;
public class InventoryTransaction
{
    [Key]
    public long Id { get; set; }
    public int? ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int PreviousQuantity { get; set; }
    public int QuantityDelta { get; set; }
    public int NewQuantity { get; set; }

    [Required]
    [StringLength(300)]
    public string Reason { get; set; } = string.Empty;
    public string? PerformedByUserId { get; set; }

    [StringLength(40)]
    public string MovementType { get; set; } = "Adjustment";
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("ProductVariantId")]
    public ProductVariant? ProductVariant { get; set; }

    [ForeignKey("PerformedByUserId")]
    public User? PerformedByUser { get; set; }
}
