using be.Models;
using be.DTOs.Checkout;

namespace be.Services.Checkout;

public static class ShippingService
{
    public static bool Eligible(ShippingMethod method, Address address, decimal subtotal, string currency, bool allowDevelopment)
    {
        var zone = method.ShippingZone;
        return method.IsActive && zone is { IsActive: true } && method.Currency == currency
            && (!method.IsDevelopmentValue || allowDevelopment)
            && !string.IsNullOrWhiteSpace(zone.Country) && Equal(zone.Country, address.Country)
            && (string.IsNullOrWhiteSpace(zone.Region) || Equal(zone.Region, address.DistrictOrProvince))
            && (string.IsNullOrWhiteSpace(zone.PostalCodePrefix) || (address.PostalCode?.StartsWith(zone.PostalCodePrefix, StringComparison.OrdinalIgnoreCase) ?? false))
            && (!zone.RequiresRegion || !string.IsNullOrWhiteSpace(address.DistrictOrProvince))
            && (!zone.RequiresPostalCode || !string.IsNullOrWhiteSpace(address.PostalCode))
            && (!zone.RequiresPhone || !string.IsNullOrWhiteSpace(address.Phone))
            && (!method.MinimumOrderValue.HasValue || subtotal >= method.MinimumOrderValue)
            && (!method.MaximumOrderValue.HasValue || subtotal <= method.MaximumOrderValue)
            && method.BasePrice >= 0 && (!method.IsPickup || !string.IsNullOrWhiteSpace(method.PickupLocation));
    }
    public static ShippingOptionDto Option(ShippingMethod method, decimal subtotal) => new(method.Id, method.Code, method.Name,
        method.FreeShippingThreshold.HasValue && subtotal >= method.FreeShippingThreshold.Value ? 0 : Money.Round(method.BasePrice, method.Currency),
        method.Currency, method.EstimatedMinimumDays, method.EstimatedMaximumDays, method.IsPickup, method.PickupLocation, method.IsDevelopmentValue);
    private static bool Equal(string left, string? right) => string.Equals(left.Trim(), right?.Trim(), StringComparison.OrdinalIgnoreCase);
}
