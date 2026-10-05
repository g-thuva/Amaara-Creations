using be.Data;
using be.DTOs.Admin;
using be.Models;
using be.Security;
using be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/admin/settings")]
    [Authorize(Policy = AppPolicies.ManageContent)]
    public class AdminSettingsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuditService _auditService;
        private static readonly string[] ForbiddenKeyParts = { "secret", "password", "token", "jwt", "connectionstring", "apikey", "api_key" };

        public AdminSettingsController(ApplicationDbContext context, IAuditService auditService)
        {
            _context = context;
            _auditService = auditService;
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<SiteSettingResponse>>> GetSettings()
        {
            return Ok(await _context.SiteSettings.AsNoTracking().OrderBy(s => s.Key).Select(s => ToResponse(s)).ToListAsync());
        }

        [HttpPut("{key}")]
        public async Task<ActionResult<SiteSettingResponse>> UpsertSetting(string key, SiteSettingRequest request)
        {
            if (!string.Equals(key, request.Key, StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(request.Key), "Route key must match request key.");
            }

            if (ForbiddenKeyParts.Any(part => request.Key.Contains(part, StringComparison.OrdinalIgnoreCase)))
            {
                ModelState.AddModelError(nameof(request.Key), "Infrastructure or secret-like settings cannot be managed here.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == request.Key);
            if (setting == null)
            {
                setting = new SiteSetting { Key = request.Key.Trim(), CreatedAt = DateTime.UtcNow };
                _context.SiteSettings.Add(setting);
            }

            setting.Value = request.Value;
            setting.ValueType = request.ValueType;
            setting.IsPublic = request.IsPublic;
            setting.Description = request.Description;
            setting.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            await _auditService.RecordAsync("upsert", nameof(SiteSetting), setting.Key, null, ToResponse(setting), HttpContext);
            return Ok(ToResponse(setting));
        }

        private static SiteSettingResponse ToResponse(SiteSetting setting)
        {
            return new SiteSettingResponse
            {
                Id = setting.Id,
                Key = setting.Key,
                Value = setting.Value,
                ValueType = setting.ValueType,
                IsPublic = setting.IsPublic,
                Description = setting.Description,
                UpdatedAt = setting.UpdatedAt
            };
        }
    }
}
