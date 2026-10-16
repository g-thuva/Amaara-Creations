using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Shipping;
public class ShippingMethod
{
    [Key]
    public int Id { get; set; }
    public int? ShippingZoneId { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? FreeShippingThreshold { get; set; }
    public bool IsActive { get; set; } = true;

    [ForeignKey("ShippingZoneId")]
    public ShippingZone? ShippingZone { get; set; }

    [StringLength(50)]
    public string Code { get; set; } = string.Empty;

    [StringLength(3)]
    public string Currency { get; set; } = "LKR";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumOrderValue { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaximumOrderValue { get; set; }
    public int? EstimatedMinimumDays { get; set; }
    public int? EstimatedMaximumDays { get; set; }
    public bool IsPickup { get; set; }

    [StringLength(500)]
    public string? PickupLocation { get; set; }
    public bool IsDevelopmentValue { get; set; }
    public int DisplayOrder { get; set; }
}
