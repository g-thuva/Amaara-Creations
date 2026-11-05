using be.Application.Features.Identity;
using Microsoft.Extensions.Configuration;

namespace be.Tests;

public sealed class SessionPolicyTests
{
    [Theory]
    [InlineData(false, 15)]
    [InlineData(true, 10)]
    public void DefaultsRemainShortAndAConfiguredShorterLegacyLimitIsPreserved(bool privileged, int minutes)
    {
        var defaults = new ConfigurationBuilder().Build();
        Assert.Equal(minutes, SessionPolicy.AccessMinutes(defaults, privileged));
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:AccessTokenMinutes"] = "5" }).Build();
        Assert.Equal(5, SessionPolicy.AccessMinutes(config, privileged));
    }
    [Theory]
    [InlineData("CustomerIdleMinutes", "0")]
    [InlineData("CustomerAbsoluteMinutes", "1")]
    [InlineData("PrivilegedAccessMinutes", "45")]
    public void InvalidOrOverlongPoliciesFailValidation(string key, string value)
    {
        var config = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Authentication:Sessions:" + key] = value }).Build();
        Assert.Throws<InvalidOperationException>(() => SessionPolicy.Validate(config));
    }
}
