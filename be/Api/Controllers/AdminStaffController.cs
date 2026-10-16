using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController, Route("api/v1/admin"), Authorize(Policy = AppPolicies.AdminAccess)]
public sealed class AdminStaffController(StaffManagementService staffManagementService) : ControllerBase
{
    [HttpGet("staff"), Authorize(Policy = AppPolicies.ManageStaff)]
    public async Task<IActionResult> Staff(int page = 1, CancellationToken ct = default)
    {
        return Ok(await staffManagementService.Staff(page, ct));
    }

    [HttpPut("staff"), Authorize(Policy = AppPolicies.ManageStaff)]
    public async Task<IActionResult> Assign(StaffRequest request, CancellationToken ct)
    {
        await staffManagementService.Assign(request, ct);
        return NoContent();
    }
}
