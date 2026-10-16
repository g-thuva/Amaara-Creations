using be.Application.Features.Identity;
using be.Application.Features.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin"), Authorize(Policy = AppPolicies.AdminAccess)]
public sealed class AdminDashboardController(DashboardQueries dashboardQueries) : ControllerBase
{
    // GET: api/admin/dashboard - Get dashboard statistics
    [HttpGet("dashboard")]
    [Authorize(Policy = AppPolicies.ViewReports)]
    public async Task<ActionResult<DashboardStatsResponse>> GetDashboardStats()
    {
        return Ok(await dashboardQueries.GetDashboardStats());
    }

    // GET: api/admin/dashboard/revenue - Get revenue statistics
    [HttpGet("dashboard/revenue")]
    [Authorize(Policy = AppPolicies.ViewReports)]
    public async Task<ActionResult<RevenueStatsResponse>> GetRevenueStats()
    {
        return Ok(await dashboardQueries.GetRevenueStats());
    }

    // GET: api/admin/dashboard/orders - Get order statistics
    [HttpGet("dashboard/orders")]
    [Authorize(Policy = AppPolicies.ViewReports)]
    public async Task<ActionResult<OrderStatsResponse>> GetOrderStats()
    {
        return Ok(await dashboardQueries.GetOrderStats());
    }

    // GET: api/admin/dashboard/products - Get product statistics
    [HttpGet("dashboard/products")]
    [Authorize(Policy = AppPolicies.ViewReports)]
    public async Task<ActionResult<ProductStatsResponse>> GetProductStats()
    {
        return Ok(await dashboardQueries.GetProductStats());
    }

    // GET: api/admin/dashboard/recent-orders - Get recent orders
    [HttpGet("dashboard/recent-orders")]
    [Authorize(Policy = AppPolicies.ViewReports)]
    public async Task<ActionResult<List<RecentOrderResponse>>> GetRecentOrders([FromQuery] int limit = 10)
    {
        return Ok(await dashboardQueries.GetRecentOrders(limit));
    }
}
