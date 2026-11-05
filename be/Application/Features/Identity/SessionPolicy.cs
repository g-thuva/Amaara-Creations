using System.Security.Cryptography;
using System.Text;
using be.Domain.Identity;

namespace be.Application.Features.Identity;

public static class SessionPolicy
{
    public static double Minutes(IConfiguration configuration, string key, double fallback)
    {
        var value = configuration.GetValue<double?>("Authentication:Sessions:" + key) ?? fallback;
        if (!double.IsFinite(value) || value <= 0) throw new InvalidOperationException("Invalid session lifetime: " + key);
        return value;
    }
    public static TimeSpan Idle(IConfiguration configuration, bool privileged) => TimeSpan.FromMinutes(Minutes(configuration, privileged ? "PrivilegedIdleMinutes" : "CustomerIdleMinutes", privileged ? 30 : 10080));
    public static TimeSpan Absolute(IConfiguration configuration, bool privileged) => TimeSpan.FromMinutes(Minutes(configuration, privileged ? "PrivilegedAbsoluteMinutes" : "CustomerAbsoluteMinutes", privileged ? 720 : 43200));
    public static double AccessMinutes(IConfiguration configuration, bool privileged)
    {
        var configured = Minutes(configuration, privileged ? "PrivilegedAccessMinutes" : "CustomerAccessMinutes", privileged ? 10 : 15);
        var legacy = configuration.GetValue<double?>("Authentication:AccessTokenMinutes") ?? configuration.GetValue<double?>("Jwt:AccessTokenMinutes");
        if (legacy.HasValue && (!double.IsFinite(legacy.Value) || legacy <= 0)) throw new InvalidOperationException("Invalid access token lifetime.");
        // Preserve a previously configured shorter security limit during rollout.
        return legacy.HasValue ? Math.Min(configured, legacy.Value) : configured;
    }
    public static void Validate(IConfiguration configuration)
    {
        foreach (var privileged in new[] { false, true })
        {
            var access = AccessMinutes(configuration, privileged);
            var idle = Idle(configuration, privileged);
            var absolute = Absolute(configuration, privileged);
            if (access > 30 || idle > absolute || absolute > TimeSpan.FromDays(365))
                throw new InvalidOperationException("Session policy requires access <= 30 minutes and idle <= absolute <= 365 days.");
        }
    }
    public static string Digest(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    public static string Roles(IEnumerable<string> roles) => Digest(string.Join('|', roles.Order(StringComparer.Ordinal)));
    public static bool Active(RefreshSession session, DateTime now) => session.RevokedAt == null && session.ExpiresAt > now && session.IdleExpiresAt > now && session.AbsoluteExpiresAt > now;
}
