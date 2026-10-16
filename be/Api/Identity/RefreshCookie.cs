using be.Application.Features.Identity;

namespace be.Api.Identity;
public sealed class RefreshCookie(IConfiguration configuration, IWebHostEnvironment environment)
{
    private string Name => configuration["Authentication:RefreshCookieName"] ?? "amaara_refresh";

    private CookieOptions Options(DateTime? expires = null) => new()
    {
        HttpOnly = true,
        Secure = !environment.IsDevelopment(),
        SameSite = SameSiteMode.Lax,
        Path = "/api/v1/auth",
        Expires = expires
    };
    public string? Read(HttpRequest request) => request.Cookies[Name];
    public void Write(HttpResponse response, AuthenticatedSession session) => response.Cookies.Append(Name, session.RefreshToken, Options(session.RefreshExpiresAt));
    public void Clear(HttpResponse response) => response.Cookies.Delete(Name, Options());
}
