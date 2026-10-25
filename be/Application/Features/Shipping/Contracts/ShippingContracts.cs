using be.Domain.Common;
using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Shipping;
public sealed class ShippingZoneRequest
{
    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string Country { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Region { get; set; }

    [StringLength(30)]
    public string? PostalCodePrefix { get; set; }
    public bool IsActive { get; set; } = true;
    public bool RequiresRegion { get; set; }
    public bool RequiresPostalCode { get; set; }
    public bool RequiresPhone { get; set; } = true;
}

public sealed class ShippingMethodRequest
{
    [Range(1, int.MaxValue)]
    public int ShippingZoneId { get; set; }

    [Required, StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(50), RegularExpression("^[A-Z0-9_-]+$")]
    public string Code { get; set; } = string.Empty;

    [Required, StringLength(3)]
    public string Currency { get; set; } = Money.DefaultCurrency;

    [Range(typeof(decimal), "0", "9999999999999999")]
    public decimal BasePrice { get; set; }
    public decimal? FreeShippingThreshold { get; set; }
    public decimal? MinimumOrderValue { get; set; }
    public decimal? MaximumOrderValue { get; set; }

    [Range(0, 365)]
    public int? EstimatedMinimumDays { get; set; }

    [Range(0, 365)]
    public int? EstimatedMaximumDays { get; set; }
    public bool IsPickup { get; set; }

    [StringLength(500)]
    public string? PickupLocation { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDevelopmentValue { get; set; }
    public int DisplayOrder { get; set; }
}

public record ShippingOptionDto(
        int Id,
        string Code,
        string Name,
        decimal Amount,
        string Currency,
        int? EstimatedMinimumDays,
        int? EstimatedMaximumDays,
        bool IsPickup,
        string? PickupLocation,
        bool IsDevelopmentValue);

public record ShipmentRequest(
        [Required] string Status,
        [StringLength(120)] string? Courier,
        [StringLength(120)] string? TrackingNumber,
        [StringLength(500)] string? TrackingUrl,
        [StringLength(500)] string? Notes = null);
