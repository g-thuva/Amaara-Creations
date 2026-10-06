using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public class OrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int OrderId { get; set; }

        public OrderItemType ItemType { get; set; } = OrderItemType.Product;

        public int? ProductId { get; set; }

        public int? ProductVariantId { get; set; }

        public int? CustomDesignId { get; set; }

        [Required]
        [StringLength(200)]
        public string ProductNameSnapshot { get; set; } = string.Empty;

        [StringLength(64)]
        public string? SkuSnapshot { get; set; }

        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity must be at least 1")]
        public int Quantity { get; set; } = 1;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; } // Price at the time of order

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal UnitPrice { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; } // Price * Quantity

        public string? ConfigurationSnapshotJson { get; set; }

        public int? CustomDesignSnapshotVersion { get; set; }

        [StringLength(500)]
        public string? ImageSnapshot { get; set; }

        // Navigation properties
        [ForeignKey("OrderId")]
        public Order? Order { get; set; }

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("ProductVariantId")]
        public ProductVariant? ProductVariant { get; set; }

        [ForeignKey("CustomDesignId")]
        public CustomDesign? CustomDesign { get; set; }
    }
}

