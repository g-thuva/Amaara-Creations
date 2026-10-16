using be.Application.Features.Identity;
using be.Application.Features.Customers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin"), Authorize(Policy = AppPolicies.AdminAccess)]
public sealed class AdminCustomersController(CustomerQueries customerQueries, CustomerOperationsQueries customerOperationsQueries) : ControllerBase
{
    // GET: api/admin/customers - Get all customers
    [HttpGet("customers")]
    [Authorize(Policy = AppPolicies.ManageCustomers)]
    public async Task<ActionResult> GetAllCustomers([FromQuery] string? search = null, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await customerQueries.GetAllCustomers(search, pageNumber, pageSize));
    }

    // GET: api/admin/customers/{id} - Get customer details
    [HttpGet("customers/{id}")]
    [Authorize(Policy = AppPolicies.ManageCustomers)]
    public async Task<ActionResult<CustomerDetailResponse>> GetCustomerDetails(string id)
    {
        return Ok(await customerQueries.GetCustomerDetails(id));
    }

    // GET: api/admin/customers/{id}/orders - Get customer orders
    [HttpGet("customers/{id}/orders")]
    [Authorize(Policy = AppPolicies.ManageCustomers)]
    public async Task<ActionResult> GetCustomerOrders(string id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
    {
        return Ok(await customerQueries.GetCustomerOrders(id, pageNumber, pageSize));
    }

    // GET: api/admin/customers/{id}/stats - Get customer statistics
    [HttpGet("customers/{id}/stats")]
    [Authorize(Policy = AppPolicies.ManageCustomers)]
    public async Task<ActionResult<CustomerStatsResponse>> GetCustomerStats(string id)
    {
        return Ok(await customerQueries.GetCustomerStats(id));
    }

    [HttpGet("customers/{id}/operations"), Authorize(Policy = AppPolicies.ManageCustomers)]
    public async Task<IActionResult> Customer(string id, CancellationToken ct)
    {
        return Ok(await customerOperationsQueries.Customer(id, ct));
    }
}
