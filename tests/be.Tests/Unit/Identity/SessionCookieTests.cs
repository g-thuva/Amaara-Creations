using be.Api.Identity;
using be.Application.Features.Identity;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace be.Tests;

public sealed class SessionCookieTests
{
    [Theory]
    [InlineData("Production", "Lax", false, true)]
    [InlineData("Production", "None", false, true)]
    [InlineData("Development", "Lax", false, false)]
    [InlineData("Development", "None", true, true)]
    public void PersistentCookiesAreHttpOnlyHostScopedAndSecureAccordingToTheEnvironment(string environment, string sameSite, bool forceSecure, bool secure)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:CookieSameSite"] = sameSite, ["Authentication:SecureCookies"] = forceSecure.ToString() }).Build();
        var cookie = new RefreshCookie(config, new TestEnvironment { EnvironmentName = environment }, new EphemeralDataProtectionProvider());
        var response = new DefaultHttpContext().Response;
        cookie.Write(response, new(new() { Token = "short-access" }, "secret-refresh", DateTime.UtcNow.AddHours(12), new("user", "stamp", "family", DateTimeOffset.UtcNow.ToUnixTimeSeconds())), true);
        var values = response.Headers.SetCookie.ToArray();
        Assert.Equal(2, values.Length);
        Assert.All(values, value =>
        {
            Assert.Contains("httponly", value!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("expires=", value!, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("path=/api/v1/auth/staff", value!);
            Assert.Contains("samesite=" + sameSite.ToLowerInvariant(), value!, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain("domain=", value!, StringComparison.OrdinalIgnoreCase);
            Assert.Equal(secure, value!.Contains("; secure", StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain("secret-refresh", value!);
        });
        cookie.Clear(response, true);
        Assert.Contains(response.Headers.SetCookie, value => value!.Contains("expires=Thu, 01 Jan 1970"));
    }

    [Fact]
    public void CrossSiteCookiesCannotSilentlyDisableSecureOnHttpDevelopment()
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:CookieSameSite"] = "None" }).Build();
        var cookie = new RefreshCookie(config, new TestEnvironment(), new EphemeralDataProtectionProvider());
        Assert.Throws<InvalidOperationException>(() => cookie.Write(new DefaultHttpContext().Response, new(new(), "refresh", DateTime.UtcNow.AddDays(1))));
    }

    [Fact]
    public void SqlTimestampsKeepTheirUtcExpiryWhenWrittenToCookiesOrClampedAccessTokens()
    {
        var config = new ConfigurationBuilder().Build();
        var expected = DateTime.UtcNow.AddMinutes(2);
        var sqlValue = DateTime.SpecifyKind(expected, DateTimeKind.Unspecified);
        var response = new DefaultHttpContext().Response;
        new RefreshCookie(config, new TestEnvironment(), new EphemeralDataProtectionProvider())
            .Write(response, new(new(), "test-refresh", sqlValue));
        var header = Microsoft.Net.Http.Headers.SetCookieHeaderValue.Parse(response.Headers.SetCookie.Single(value => value!.StartsWith("amaara_refresh="))!);
        Assert.Equal(new DateTimeOffset(expected).ToUnixTimeSeconds(), header.Expires!.Value.ToUnixTimeSeconds());
        var accessExpiry = new be.Infrastructure.Identity.TokenService(config).GetAccessTokenExpiry(sessionExpiresAt: sqlValue);
        Assert.Equal(DateTimeKind.Utc, accessExpiry.Kind);
        Assert.Equal(expected, accessExpiry);
    }

    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Development";
        public string ApplicationName { get; set; } = "SessionCookieTests";
        public string ContentRootPath { get; set; } = "";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = "";
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
