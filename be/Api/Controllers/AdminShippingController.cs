using be.Application.Features.Identity;
using be.Application.Features.Checkout;
using be.Application.Features.Shipping;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin/shipping"), Authorize(Policy = AppPolicies.ManageOrders)]
public sealed class AdminShippingController(ShippingManagementService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        return Ok(await service.Get(ct));
    }

    [HttpPost("zones")]
    public async Task<IActionResult> CreateZone(ShippingZoneRequest request, CancellationToken ct)
    {
        return Ok(await service.CreateZone(request, ct));
    }

    [HttpPut("zones/{id:int}")]
    public async Task<IActionResult> UpdateZone(int id, ShippingZoneRequest request, CancellationToken ct)
    {
        return Ok(await service.UpdateZone(id, request, ct));
    }

    [HttpPost("methods")]
    public async Task<IActionResult> CreateMethod(ShippingMethodRequest request, CancellationToken ct)
    {
        return Ok(await service.CreateMethod(request, ct));
    }

    [HttpPut("methods/{id:int}")]
    public async Task<IActionResult> UpdateMethod(int id, ShippingMethodRequest request, CancellationToken ct)
    {
        return Ok(await service.UpdateMethod(id, request, ct));
    }

    [HttpPut("orders/{orderId:int}/shipment")]
    public async Task<IActionResult> Shipment(int orderId, ShipmentRequest request, CancellationToken ct)
    {
        return Ok(await service.Shipment(orderId, request, ct));
    }
}
