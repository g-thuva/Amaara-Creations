using be.Application.Features.Identity;
using be.Application.Features.Orders;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/orders"), Authorize]
public sealed class OrdersController(OrderService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<OrderResponse>>> GetUserOrders(CancellationToken ct)
    {
        return Ok(await service.GetUserOrders(ct));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetOrder(int id, CancellationToken ct)
    {
        return Ok(await service.GetOrder(id, ct));
    }

    [HttpPost]
    public IActionResult CreateOrder()
    {
        return Problem(statusCode: 410, title: "Use the authoritative checkout quote and payment session endpoints.");
    }

    [HttpGet("/api/v1/admin/orders")]
    [Authorize(Policy = AppPolicies.ViewOrders)]
    public async Task<IActionResult> GetAllOrders(string? status = null, string? search = null, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default)
    {
        return Ok(await service.GetAllOrders(status, search, pageNumber, pageSize, ct));
    }

    [HttpGet("/api/v1/admin/orders/{id:int}")]
    [Authorize(Policy = AppPolicies.ViewOrders)]
    public async Task<ActionResult<OrderResponse>> GetAdminOrder(int id, CancellationToken ct)
    {
        return Ok(await service.GetAdminOrder(id, ct));
    }

    [HttpPut("/api/v1/admin/orders/{id:int}/status")]
    [Authorize(Policy = AppPolicies.ManageOrders)]
    public async Task<ActionResult<OrderResponse>> UpdateOrderStatus(int id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        return Ok(await service.UpdateOrderStatus(id, request, ct));
    }
}
