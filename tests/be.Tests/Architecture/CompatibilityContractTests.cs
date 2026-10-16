using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using be.Api.Controllers;
using be.Application.Features.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.RateLimiting;

namespace be.Tests.Architecture;
public sealed class CompatibilityContractTests
{
    [Fact]
    public void EveryAuthorizationPolicyRetainsItsOriginalRoleAssignments()
    {
        var baseline = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(Fixture("authorization-policies.json")))!;
        Assert.Equal(baseline.Keys.Order(StringComparer.Ordinal), AppPolicies.Roles.Keys.Order(StringComparer.Ordinal));
        foreach (var entry in baseline)
            Assert.Equal(entry.Value, AppPolicies.Roles[entry.Key]);
    }

    [Fact]
    public void EveryExistingEndpointRetainsItsRouteAuthorizationAndRequestLimits()
    {
        using var baseline = JsonDocument.Parse(File.ReadAllText(Fixture("api-contract.json")));
        var expected = baseline.RootElement.EnumerateArray().Select(element => element.GetRawText()).ToArray();
        var actual = typeof(AuthController).Assembly.GetTypes().Where(type => typeof(ControllerBase).IsAssignableFrom(type) && !type.IsAbstract).SelectMany(ControllerEntries).OrderBy(entry => entry.path, StringComparer.Ordinal).ThenBy(entry => entry.method, StringComparer.Ordinal).Select(entry => JsonSerializer.Serialize(entry)).ToArray();
        Assert.Equal(expected.Select(Normalize), actual.Select(Normalize));
    }

    [Fact]
    public void HistoricalMigrationAndSnapshotPayloadsRemainUnchanged()
    {
        var baseline = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(Fixture("migration-history.json")))!;
        foreach (var entry in baseline)
        {
            var source = File.ReadAllText(Path.Combine(RepositoryRoot.Path, "be", "Infrastructure", "Persistence", "Migrations", entry.Key));
            // Namespace imports can follow a CLR type move; historical statements cannot.
            var normalized = Regex.Replace(source, @"(?m)^using[^\r\n]*;\s*$", "");
            normalized = string.Join("\n", normalized.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0));
            Assert.Equal(entry.Value, Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized))));
        }
    }

    private static string Fixture(string name) => Path.Combine(RepositoryRoot.Path, "tests", "be.Tests", "Architecture", "Fixtures", name);
    private static string Normalize(string value) => JsonSerializer.Serialize(JsonSerializer.Deserialize<JsonElement>(value));
    private static IEnumerable<RouteEntry> ControllerEntries(Type controller)
    {
        var prefix = controller.GetCustomAttributes<RouteAttribute>().Single().Template!.Trim('/');
        foreach (var action in controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
        {
            var authorize = controller.GetCustomAttributes<AuthorizeAttribute>().Concat(action.GetCustomAttributes<AuthorizeAttribute>()).ToArray();
            var limits = controller.GetCustomAttributes<EnableRateLimitingAttribute>().Concat(action.GetCustomAttributes<EnableRateLimitingAttribute>()).Select(limit => limit.PolicyName ?? throw new InvalidOperationException("A named rate-limit policy is required.")).Distinct().Order(StringComparer.Ordinal).ToArray();
            foreach (var http in action.GetCustomAttributes().OfType<HttpMethodAttribute>())
            {
                var suffix = http.Template ?? string.Empty;
                var path = (suffix.StartsWith('/') ? suffix : prefix + "/" + suffix).Trim('/');
                foreach (var verb in http.HttpMethods)
                    yield return new(verb.ToUpperInvariant(), path, authorize.Length > 0, controller.IsDefined(typeof(AllowAnonymousAttribute)) || action.IsDefined(typeof(AllowAnonymousAttribute)), authorize.Where(a => a.Policy != null).Select(a => a.Policy!).Distinct().Order(StringComparer.Ordinal).ToArray(), authorize.Where(a => a.Roles != null).Select(a => a.Roles!).Distinct().Order(StringComparer.Ordinal).ToArray(), limits, RequestLimit(action), action.GetCustomAttribute<RequestFormLimitsAttribute>()?.MultipartBodyLengthLimit.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        }
    }

    private static string? RequestLimit(MethodInfo action)
    {
        var attribute = action.CustomAttributes.SingleOrDefault(a => a.AttributeType == typeof(RequestSizeLimitAttribute));
        return attribute == null ? null : Convert.ToString(attribute.ConstructorArguments[0].Value, System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed record RouteEntry(string method, string path, bool authenticated, bool anonymous, string[] policies, string[] roles, string[] rateLimits, string? requestLimit, string? multipartLimit);
}
