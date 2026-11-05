using be.Application.Features.Identity;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace be.Api.Identity;
public sealed class RefreshCookie(IConfiguration configuration, IWebHostEnvironment environment, IDataProtectionProvider protection)
{
    private string Name => configuration["Authentication:RefreshCookieName"] ?? "amaara_refresh";
    private string CookieName(bool management) => management ? Name + "_staff" : Name;
    private string AssuranceName(bool management) => CookieName(management) + "_assurance";
    private IDataProtector Protector => protection.CreateProtector("Amaara.StaffAssurance.v1");
    private IDataProtector ManagementProtector => protection.CreateProtector("Amaara.ManagementRefresh.v1");
    private string PendingName(bool management) => CookieName(management) + "_signin";
    public string? ReadPending(HttpRequest request, bool management = false) => request.Cookies[PendingName(management)];
    public void WritePending(HttpResponse response, string ticket, DateTimeOffset expires, bool management = false) => response.Cookies.Append(PendingName(management), ticket, Options(expires.UtcDateTime, management));
    public void ClearPending(HttpResponse response, bool management = false) => response.Cookies.Delete(PendingName(management), Options(management: management));

    private SameSiteMode SameSite => Enum.TryParse<SameSiteMode>(configuration["Authentication:CookieSameSite"] ?? "Lax", true, out var mode) && Enum.IsDefined(mode) && mode != SameSiteMode.Unspecified ? mode : throw new InvalidOperationException("Invalid authentication cookie SameSite policy.");
    private bool Secure => !environment.IsDevelopment() || configuration.GetValue<bool>("Authentication:SecureCookies");
    private CookieOptions Options(DateTime? expires = null, bool management = false) => new()
    {
        HttpOnly = true,
        Secure = Secure,
        SameSite = SameSite == SameSiteMode.None && !Secure ? throw new InvalidOperationException("Cross-site cookies require HTTPS and SecureCookies.") : SameSite,
        Path = management ? "/api/v1/auth/staff" : "/api/v1/auth",
        Expires = expires.HasValue ? new DateTimeOffset(DateTime.SpecifyKind(expires.Value, DateTimeKind.Utc)) : null
    };
    public string? Read(HttpRequest request, bool management = false)
    {
        var value = request.Cookies[CookieName(management)];
        if (!management || value == null) return value;
        try { return ManagementProtector.Unprotect(value); }
        catch (System.Security.Cryptography.CryptographicException) { return null; }
    }
    public StaffAssurance? ReadAssurance(HttpRequest request, bool management = false)
    {
        if (!request.Cookies.TryGetValue(AssuranceName(management), out var value)) return null;
        try { return JsonSerializer.Deserialize<StaffAssurance>(Protector.Unprotect(value)); }
        catch (Exception error) when (error is System.Security.Cryptography.CryptographicException or JsonException) { return null; }
    }
    public void Write(HttpResponse response, AuthenticatedSession session, bool management = false)
    {
        response.Cookies.Append(CookieName(management), management ? ManagementProtector.Protect(session.RefreshToken) : session.RefreshToken, Options(session.RefreshExpiresAt, management));
        if (session.StaffAssurance is { } grant)
            response.Cookies.Append(AssuranceName(management), Protector.Protect(JsonSerializer.Serialize(grant)), Options(session.RefreshExpiresAt, management));
        else response.Cookies.Delete(AssuranceName(management), Options(management: management));
    }
    public void Clear(HttpResponse response, bool management = false)
    {
        response.Cookies.Delete(CookieName(management), Options(management: management));
        response.Cookies.Delete(AssuranceName(management), Options(management: management));
    }
    public void ClearAll(HttpResponse response) { Clear(response); Clear(response, true); ClearPending(response); ClearPending(response, true); }
}
