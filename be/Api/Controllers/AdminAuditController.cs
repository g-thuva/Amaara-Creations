using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be.Application.Features.Audit;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin"), Authorize(Policy = AppPolicies.AdminAccess)]
public sealed class AdminAuditController(AuditQueries auditQueries) : ControllerBase
{
    [HttpGet("audit"), Authorize(Policy = AppPolicies.ViewAudit)]
    public async Task<IActionResult> Audit(string? action, string? entityType, string? entityId, int page = 1, CancellationToken ct = default)
    {
        return Ok(await auditQueries.Audit(action, entityType, entityId, page, ct));
    }
}
