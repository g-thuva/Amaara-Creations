using be.Application.Features.Identity;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace be.Api.Identity;
public sealed class RefreshCookie(IConfiguration configuration, IWebHostEnvironment environment, IDataProtectionProvider protection)
{
    private string Name => configuration["Authentication:RefreshCookieName"] ?? "amaara_refresh";
    private string StaffName => Name + "_assurance";
    private IDataProtector Protector => protection.CreateProtector("Amaara.StaffAssurance.v1");

    private CookieOptions Options(DateTime? expires = null) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Path = "/api/v1/auth",
        Expires = expires
    };
    public string? Read(HttpRequest request) => request.Cookies[Name];
    public StaffAssurance? ReadAssurance(HttpRequest request)
    {
        if (!request.Cookies.TryGetValue(StaffName, out var value)) return null;
        try { return JsonSerializer.Deserialize<StaffAssurance>(Protector.Unprotect(value)); }
        catch (Exception error) when (error is System.Security.Cryptography.CryptographicException or JsonException) { return null; }
    }
    public void Write(HttpResponse response, AuthenticatedSession session)
    {
        response.Cookies.Append(Name, session.RefreshToken, Options(session.RefreshExpiresAt));
        if (session.StaffAssurance is { } grant)
            response.Cookies.Append(StaffName, Protector.Protect(JsonSerializer.Serialize(grant)), Options(DateTimeOffset.FromUnixTimeSeconds(grant.VerifiedAt).AddMinutes(15).UtcDateTime));
        else response.Cookies.Delete(StaffName, Options());
    }
    public void Clear(HttpResponse response)
    {
        response.Cookies.Delete(Name, Options());
        response.Cookies.Delete(StaffName, Options());
    }
}
