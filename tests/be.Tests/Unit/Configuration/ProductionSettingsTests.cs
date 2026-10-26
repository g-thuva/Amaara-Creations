using be.Infrastructure.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.AspNetCore.Hosting;

namespace be.Tests;

public sealed class ProductionSettingsTests
{
    [Fact]
    public void ProductionManagementRequiresItsOwnExactHttpsOrigin()
    {
        var values = Valid(); values["Administration:Enabled"] = "true";
        Assert.Throws<InvalidOperationException>(() => ProductionSettings.Validate(Config(values), new TestEnvironment()));
        values["Administration:AllowedOrigins:0"] = "http://staff.example.test";
        Assert.Throws<InvalidOperationException>(() => ProductionSettings.Validate(Config(values), new TestEnvironment()));
        values["Administration:AllowedOrigins:0"] = "https://staff.example.test";
        ProductionSettings.Validate(Config(values), new TestEnvironment());
    }
    [Theory]
    [InlineData("AllowedHosts", "*")]
    [InlineData("AllowedHosts", "<APP_HOST>")]
    [InlineData("DataProtection:KeysPath", "")]
    [InlineData("Authentication:FrontendBaseUrl", "http://store.example.test")]
    [InlineData("Checkout:AllowDevelopmentShipping", "true")]
    [InlineData("Uploads:Provider", "S3")]
    [InlineData("Email:Enabled", "true")]
    [InlineData("Zahls:Enabled", "true")]
    [InlineData("LocalPaymentSimulator:Enabled", "true")]
    [InlineData("Payments:Provider", "LocalSimulator")]
    public void ProductionRejectsUnsafeOrIncompleteEnabledSettings(string key, string value)
    {
        var values = Valid(); values[key] = value;
        Assert.ThrowsAny<Exception>(() => ProductionSettings.Validate(Config(values), new TestEnvironment()));
    }

    [Fact]
    public void ProductionRejectsDataProtectionKeysUnderThePublicRoot()
    {
        var values = Valid();
        values["DataProtection:KeysPath"] = Path.Combine(Path.GetTempPath(), "wwwroot", "keys");
        Assert.Throws<InvalidOperationException>(() => ProductionSettings.Validate(Config(values), new TestEnvironment()));
    }

    [Fact]
    public void DisabledOptionalIntegrationsAllowAnExplicitProductionHost()
        => ProductionSettings.Validate(Config(Valid()), new TestEnvironment());

    [Theory]
    [InlineData("")]
    [InlineData("App_Data")]
    public void PublicRootCannotExposeApplicationConfigurationOrPrivateMedia(string relative)
    {
        var environment = new TestEnvironment { WebRootPath = Path.Combine(Path.GetTempPath(), relative) };
        Assert.Throws<InvalidOperationException>(() => ProductionSettings.Validate(Config(Valid()), environment));
    }

    private static Dictionary<string, string?> Valid() => new()
    {
        ["AllowedHosts"] = "store.example.test", ["DataProtection:KeysPath"] = "/private/rehearsal-keys",
        ["Authentication:FrontendBaseUrl"] = "https://store.example.test", ["Email:Enabled"] = "false",
        ["Uploads:Provider"] = "Local", ["Storage:AllowLocalInProduction"] = "true", ["Zahls:Enabled"] = "false"
    };
    private static IConfiguration Config(Dictionary<string, string?> values) => new ConfigurationBuilder().AddInMemoryCollection(values).Build();
    private sealed class TestEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;
        public string ApplicationName { get; set; } = "be.Tests";
        public string ContentRootPath { get; set; } = Path.GetTempPath();
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string WebRootPath { get; set; } = Path.Combine(Path.GetTempPath(), "wwwroot") + Path.DirectorySeparatorChar;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
