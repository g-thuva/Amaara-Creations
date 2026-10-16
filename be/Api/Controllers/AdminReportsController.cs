using be.Application.Features.Identity;
using be.Application.Features.Reporting;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin"), Authorize(Policy = AppPolicies.AdminAccess)]
public sealed class AdminReportsController(ReportingQueries reportingQueries) : ControllerBase
{
    [HttpGet("reports"), Authorize(Policy = AppPolicies.ViewReports)]
    public async Task<IActionResult> Reports(DateTime? from, DateTime? to, CancellationToken ct)
    {
        return Ok(await reportingQueries.Reports(from, to, ct));
    }

    [HttpGet("reports/orders.csv"), Authorize(Policy = AppPolicies.ViewReports)]
    public async Task<IActionResult> Export(DateTime? from, DateTime? to, CancellationToken ct)
    {
        var result = await reportingQueries.Export(from, to, ct);
        return File(result.Content, result.ContentType, result.FileName, result.EnableRangeProcessing);
    }
}
