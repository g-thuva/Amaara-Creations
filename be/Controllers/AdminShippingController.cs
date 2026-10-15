using System.Security.Claims;
using be.Data;
using be.DTOs.Checkout;
using be.Models;
using be.Security;
using be.Services.Checkout;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController, Route("api/v1/admin/shipping"), Authorize(Policy = AppPolicies.ManageOrders)]
public sealed class AdminShippingController(ApplicationDbContext db, OrderLifecycleService lifecycle) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct) => Ok(new
    {
        Zones = await db.ShippingZones.AsNoTracking().Select(z => new { z.Id, z.Name, z.Country, z.Region, z.PostalCodePrefix, z.IsActive, z.RequiresPhone, z.RequiresPostalCode, z.RequiresRegion }).ToListAsync(ct),
        Methods = await db.ShippingMethods.AsNoTracking().Select(m => new { m.Id, m.ShippingZoneId, m.Name, m.Code, m.Currency, m.BasePrice, m.FreeShippingThreshold, m.MinimumOrderValue, m.MaximumOrderValue, m.EstimatedMinimumDays, m.EstimatedMaximumDays, m.IsActive, m.IsPickup, m.PickupLocation, m.IsDevelopmentValue, m.DisplayOrder }).ToListAsync(ct)
    });
    [HttpPost("zones")]
    public Task<IActionResult> CreateZone(ShippingZoneRequest request, CancellationToken ct) => SaveZone(null, request, ct);
    [HttpPut("zones/{id:int}")]
    public Task<IActionResult> UpdateZone(int id, ShippingZoneRequest request, CancellationToken ct) => SaveZone(id, request, ct);
    private async Task<IActionResult> SaveZone(int? id, ShippingZoneRequest r, CancellationToken ct)
    {
        await using var tx = await CommerceTransaction.BeginAsync(db, ct);
        var zone = id.HasValue ? await db.ShippingZones.SingleOrDefaultAsync(z => z.Id == id, ct) ?? throw new CommerceException("SHIPPING_ZONE_NOT_FOUND", "Shipping zone not found.", 404) : new ShippingZone();
        if (await db.ShippingZones.AnyAsync(z => z.Name == r.Name.Trim() && z.Id != zone.Id, ct)) throw new CommerceException("SHIPPING_ZONE_DUPLICATE", "A zone with this name already exists.", 409);
        zone.Name = r.Name.Trim(); zone.Country = r.Country.Trim(); zone.Region = r.Region?.Trim(); zone.PostalCodePrefix = r.PostalCodePrefix?.Trim(); zone.IsActive = r.IsActive; zone.RequiresPhone = r.RequiresPhone; zone.RequiresRegion = r.RequiresRegion; zone.RequiresPostalCode = r.RequiresPostalCode;
        if (!id.HasValue) db.ShippingZones.Add(zone);
        Audit("ShippingZoneChanged", id?.ToString()); await db.SaveChangesAsync(ct); if (tx != null) await tx.CommitAsync(ct);
        return Ok(new { zone.Id });
    }
    [HttpPost("methods")]
    public Task<IActionResult> CreateMethod(ShippingMethodRequest request, CancellationToken ct) => SaveMethod(null, request, ct);
    [HttpPut("methods/{id:int}")]
    public Task<IActionResult> UpdateMethod(int id, ShippingMethodRequest request, CancellationToken ct) => SaveMethod(id, request, ct);
    private async Task<IActionResult> SaveMethod(int? id, ShippingMethodRequest r, CancellationToken ct)
    {
        _ = Money.Exponent(r.Currency);
        if (r.BasePrice < 0 || r.FreeShippingThreshold < 0 || r.MinimumOrderValue < 0 || r.MaximumOrderValue < 0 || r.MaximumOrderValue < r.MinimumOrderValue || r.EstimatedMaximumDays < r.EstimatedMinimumDays || r.IsPickup && string.IsNullOrWhiteSpace(r.PickupLocation))
            throw new CommerceException("SHIPPING_CONFIGURATION_INVALID", "Rates, thresholds, estimates and pickup location must be valid.");
        await using var tx = await CommerceTransaction.BeginAsync(db, ct);
        if (!await db.ShippingZones.AnyAsync(z => z.Id == r.ShippingZoneId, ct)) throw new CommerceException("SHIPPING_ZONE_NOT_FOUND", "Shipping zone not found.", 404);
        var m = id.HasValue ? await db.ShippingMethods.SingleOrDefaultAsync(m => m.Id == id, ct) ?? throw new CommerceException("SHIPPING_METHOD_NOT_FOUND", "Shipping method not found.", 404) : new ShippingMethod();
        if (await db.ShippingMethods.AnyAsync(m => m.Code == r.Code && m.Id != (id ?? 0), ct)) throw new CommerceException("SHIPPING_CODE_DUPLICATE", "This delivery code already exists.", 409);
        m.ShippingZoneId = r.ShippingZoneId; m.Name = r.Name.Trim(); m.Code = r.Code; m.Currency = r.Currency; m.BasePrice = r.BasePrice;
        m.FreeShippingThreshold = r.FreeShippingThreshold; m.MinimumOrderValue = r.MinimumOrderValue; m.MaximumOrderValue = r.MaximumOrderValue;
        m.EstimatedMinimumDays = r.EstimatedMinimumDays; m.EstimatedMaximumDays = r.EstimatedMaximumDays; m.IsPickup = r.IsPickup; m.PickupLocation = r.PickupLocation;
        m.IsActive = r.IsActive; m.IsDevelopmentValue = r.IsDevelopmentValue; m.DisplayOrder = r.DisplayOrder;
        if (!id.HasValue) db.ShippingMethods.Add(m);
        Audit("ShippingMethodChanged", id?.ToString()); await db.SaveChangesAsync(ct); if (tx != null) await tx.CommitAsync(ct);
        return Ok(new { m.Id });
    }
    [HttpPut("orders/{orderId:int}/shipment")]
    public async Task<IActionResult> Shipment(int orderId, ShipmentRequest request, CancellationToken ct)
    {
        var s = await lifecycle.UpdateShipmentAsync(orderId, User.FindFirstValue(ClaimTypes.NameIdentifier)!, request, ct);
        return Ok(new { s.Id, Status = s.Status.ToString(), s.Courier, s.TrackingNumber, s.TrackingUrl, s.ShippedAt, s.DeliveredAt });
    }
    private void Audit(string action, string? id) => db.AuditLogs.Add(new AuditLog { ActorUserId = User.FindFirstValue(ClaimTypes.NameIdentifier), Action = action, EntityType = "Shipping", EntityId = id });
}
