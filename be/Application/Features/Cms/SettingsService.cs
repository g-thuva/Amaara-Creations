using be.Application.Abstractions.Audit;
using be.Domain.Cms;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Application.Common;

namespace be.Application.Features.Cms;
public class SettingsService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _auditService;
    private static readonly string[] ForbiddenKeyParts =
    {
        "secret",
        "password",
        "token",
        "jwt",
        "connectionstring",
        "apikey",
        "api_key"
    };
    public SettingsService(IApplicationDbContext context, IAuditService auditService, IRequestActor actor)
    {
        _context = context;
        _auditService = auditService;
        _actor = actor;
    }

    public async Task<IReadOnlyList<SiteSettingResponse>> GetSettings()
    {
        return await _context.SiteSettings.AsNoTracking().OrderBy(s => s.Key).Select(s => ToResponse(s)).ToListAsync();
    }

    public async Task<SiteSettingResponse> UpsertSetting(string key, SiteSettingRequest request)
    {
        var validation = new RequestValidation();
        if (!string.Equals(key, request.Key, StringComparison.OrdinalIgnoreCase))
        {
            validation.AddModelError(nameof(request.Key), "Route key must match request key.");
        }

        if (ForbiddenKeyParts.Any(part => request.Key.Contains(part, StringComparison.OrdinalIgnoreCase)))
        {
            validation.AddModelError(nameof(request.Key), "Infrastructure or secret-like settings cannot be managed here.");
        }

        var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == request.Key);
        if (setting == null)
        {
            setting = new SiteSetting
            {
                Key = request.Key.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.SiteSettings.Add(setting);
        }

        setting.Value = request.Value;
        setting.ValueType = request.ValueType;
        setting.IsPublic = request.IsPublic;
        setting.Description = request.Description;
        setting.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        await _auditService.RecordAsync("upsert", nameof(SiteSetting), setting.Key, null, ToResponse(setting), _actor.Audit);
        return ToResponse(setting);
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

    private readonly IRequestActor _actor;
}
