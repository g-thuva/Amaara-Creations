using System.ComponentModel.DataAnnotations;
namespace be.DTOs.Checkout;
public sealed class ShippingZoneRequest
{
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Country { get; set; } = string.Empty;
    [StringLength(100)] public string? Region { get; set; }
    [StringLength(30)] public string? PostalCodePrefix { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresRegion { get; set; }
    public bool RequiresPostalCode { get; set; }
    public bool RequiresPhone { get; set; } = true;
}
public sealed class ShippingMethodRequest
{
    [Range(1, int.MaxValue)] public int ShippingZoneId { get; set; }
    [Required, StringLength(120)] public string Name { get; set; } = string.Empty;
    [Required, StringLength(50), RegularExpression("^[A-Z0-9_-]+$")] public string Code { get; set; } = string.Empty;
    [Required, StringLength(3)] public string Currency { get; set; } = "LKR";
    [Range(typeof(decimal), "0", "9999999999999999")] public decimal BasePrice { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public decimal? MinimumOrderValue { get; set; }
    public decimal? MaximumOrderValue { get; set; }
    [Range(0, 365)] public int? EstimatedMinimumDays { get; set; }
    [Range(0, 365)] public int? EstimatedMaximumDays { get; set; }
    public bool IsPickup { get; set; }
    [StringLength(500)] public string? PickupLocation { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDevelopmentValue { get; set; }
    public int DisplayOrder { get; set; }
}
