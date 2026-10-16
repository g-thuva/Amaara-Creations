using System.ComponentModel.DataAnnotations;

namespace be.Domain.Shipping;
public class ShippingZone
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public ICollection<ShippingMethod> ShippingMethods { get; set; } = new List<ShippingMethod>();

    [StringLength(80)]
    public string Country { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Region { get; set; }

    [StringLength(30)]
    public string? PostalCodePrefix { get; set; }
    public bool RequiresRegion { get; set; }
    public bool RequiresPostalCode { get; set; }
    public bool RequiresPhone { get; set; } = true;
}
