using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using be.Application.Features.Cms;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/admin/settings")]
[Authorize(Policy = AppPolicies.ManageSettings)]
public class AdminSettingsController(SettingsService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SiteSettingResponse>>> GetSettings()
    {
        return Ok(await service.GetSettings());
    }

    [HttpPut("{key}")]
    public async Task<ActionResult<SiteSettingResponse>> UpsertSetting(string key, SiteSettingRequest request)
    {
        return Ok(await service.UpsertSetting(key, request));
    }
}
