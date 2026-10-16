using be.Domain.Catalog;
using be.Domain.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Inventory;
public class InventoryReservation
{
    [Key]
    public long Id { get; set; }
    public int ProductId { get; set; }
    public int? ProductVariantId { get; set; }
    public int OrderId { get; set; }
    public int Quantity { get; set; }

    [Required]
    [StringLength(50)]
    public string Status { get; set; } = "Reserved"; // Reserved, Consumed, Released
    public DateTime ReservedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? ConsumedAt { get; set; }
    public DateTime? ReleasedAt { get; set; }

    [ForeignKey("ProductId")]
    public Product? Product { get; set; }

    [ForeignKey("ProductVariantId")]
    public ProductVariant? ProductVariant { get; set; }

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }
}
