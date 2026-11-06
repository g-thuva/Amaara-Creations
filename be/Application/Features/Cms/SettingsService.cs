using be.Application.Abstractions.Audit;
using be.Domain.Cms;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Application.Common;
using System.ComponentModel.DataAnnotations;
using be.Domain.Common;

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
        await using var transaction = await _context.BeginCommerceTransactionAsync(CancellationToken.None);
        var validation = new RequestValidation();
        if (!string.Equals(key, request.Key, StringComparison.OrdinalIgnoreCase))
        {
            validation.AddModelError(nameof(request.Key), "Route key must match request key.");
        }

        if (ForbiddenKeyParts.Any(part => request.Key.Contains(part, StringComparison.OrdinalIgnoreCase)))
        {
            validation.AddModelError(nameof(request.Key), "Infrastructure or secret-like settings cannot be managed here.");
        }

        var definition = StoreSettingCatalog.All.SingleOrDefault(item => item.Key == request.Key);
        if (definition == null) validation.AddModelError(nameof(request.Key), "Choose a supported storefront setting.");
        if (definition != null)
        {
            if (string.IsNullOrWhiteSpace(request.Value) || request.Value.Length > definition.MaxLength)
                validation.AddModelError(nameof(request.Value), $"Enter a value of 1 to {definition.MaxLength} characters.");
            if (definition.Format == "email" && !new EmailAddressAttribute().IsValid(request.Value))
                validation.AddModelError(nameof(request.Value), "Enter a valid contact email.");
            if (definition.Format is "url" or "link")
            {
                var relative = definition.Format == "link" && request.Value.StartsWith('/') && !request.Value.StartsWith("//") && !request.Value.Contains('\\') && !request.Value.Any(char.IsControl);
                var absolute = Uri.TryCreate(request.Value, UriKind.Absolute, out var uri) && uri.Scheme == "https" && string.IsNullOrEmpty(uri.UserInfo);
                if (!relative && !absolute) validation.AddModelError(nameof(request.Value), "Use an HTTPS URL or, where supported, a storefront path beginning with a single slash.");
            }
        }
        // Validation must run before any mutation; previously errors were collected but never thrown.
        if (!validation.IsValid) throw validation.Exception();

        var setting = await _context.SiteSettings.FirstOrDefaultAsync(s => s.Key == request.Key);
        if (setting != null && request.ExpectedUpdatedAt.HasValue && setting.UpdatedAt != request.ExpectedUpdatedAt)
            throw new CommerceException("SETTING_CHANGED", "This setting changed. Reload it before saving.", 409);
        var previous = setting == null ? null : ToResponse(setting);
        if (setting == null)
        {
            setting = new SiteSetting
            {
                Key = request.Key.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.SiteSettings.Add(setting);
        }

        setting.Value = request.Value.Trim();
        setting.ValueType = "text";
        setting.IsPublic = request.IsPublic;
        setting.Description = request.Description;
        setting.UpdatedAt = DateTime.UtcNow;
        await _auditService.RecordAsync("upsert", nameof(SiteSetting), setting.Key, previous, ToResponse(setting), _actor.Audit);
        if (transaction != null) await transaction.CommitAsync();
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
