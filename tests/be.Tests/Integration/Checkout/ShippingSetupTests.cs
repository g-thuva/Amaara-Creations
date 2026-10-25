using be.Application.Features.Checkout;
using be.Domain.Shipping;
using be.Infrastructure.Persistence.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace be.Tests;

public sealed class ShippingSetupTests
{
    private static async Task Seed(CommerceFixture fixture, string environment = "Development")
    {
        using var services = new ServiceCollection()
            .AddSingleton(fixture.Db)
            .AddSingleton<IHostEnvironment>(new TestEnvironment { EnvironmentName = environment })
            .AddSingleton<IOptions<CheckoutOptions>>(Options.Create(fixture.Settings))
            .BuildServiceProvider();
        await ShippingDevelopmentSeeder.SeedAsync(services);
    }

    private static async Task RemoveShipping(CommerceFixture fixture)
    {
        fixture.Db.ShippingMethods.RemoveRange(fixture.Db.ShippingMethods);
        fixture.Db.ShippingZones.RemoveRange(fixture.Db.ShippingZones);
        await fixture.Db.SaveChangesAsync();
    }

    [Fact]
    public async Task EmptyDevelopmentStoreOffersEditableDeliveryRatesAndStandardFreeShipping()
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync();
        await RemoveShipping(fixture);
        await Seed(fixture);

        var quote = (await fixture.Pricing.CalculateAsync("a", new() { AddressId = 1 }, default)).Quote;
        Assert.Equal(new[] { "STANDARD", "EXPRESS" }, quote.AvailableShippingMethods.Select(method => method.Code));
        Assert.Equal(new decimal[] { 350, 550 }, quote.AvailableShippingMethods.Select(method => method.Amount));
        Assert.All(quote.AvailableShippingMethods, method => Assert.False(method.IsDevelopmentValue));

        var standard = quote.AvailableShippingMethods.Single(method => method.Code == "STANDARD");
        var selected = (await fixture.Pricing.CalculateAsync("a", new() { AddressId = 1, ShippingMethodId = standard.Id }, default)).Quote;
        Assert.Equal(350, selected.Shipping);
        Assert.True(selected.CanPay);

        (await fixture.Db.Products.SingleAsync()).Price = 2500;
        await fixture.Db.SaveChangesAsync();
        var free = (await fixture.Pricing.CalculateAsync("a", new() { AddressId = 1, ShippingMethodId = standard.Id }, default)).Quote;
        Assert.Equal(5000, free.Subtotal);
        Assert.Equal(0, free.Shipping);
        Assert.Equal(550, free.AvailableShippingMethods.Single(method => method.Code == "EXPRESS").Amount);
    }

    [Fact]
    public async Task RestartPreservesAdminEditsAndDisabledMethods()
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync();
        await RemoveShipping(fixture);
        await Seed(fixture);
        var standard = await fixture.Db.ShippingMethods.SingleAsync(method => method.Code == "STANDARD");
        standard.BasePrice = 425;
        standard.FreeShippingThreshold = null;
        standard.IsActive = false;
        await fixture.Db.SaveChangesAsync();

        await Seed(fixture);

        Assert.Equal(2, await fixture.Db.ShippingMethods.CountAsync());
        Assert.Equal(425, standard.BasePrice);
        Assert.Null(standard.FreeShippingThreshold);
        Assert.False(standard.IsActive);
        var quote = (await fixture.Pricing.CalculateAsync("a", new() { AddressId = 1 }, default)).Quote;
        Assert.Equal("EXPRESS", Assert.Single(quote.AvailableShippingMethods).Code);
    }

    [Theory]
    [InlineData("Production", "LKR")]
    [InlineData("Staging", "LKR")]
    [InlineData("Development", "CHF")]
    public async Task OtherEnvironmentsAndCurrenciesAreNotSeeded(string environment, string currency)
    {
        await using var fixture = new CommerceFixture();
        fixture.Settings.Currency = currency;
        await Seed(fixture, environment);
        Assert.Empty(fixture.Db.ShippingZones);
        Assert.Empty(fixture.Db.ShippingMethods);
    }

    [Fact]
    public async Task ExistingCustomZoneIsNotGivenUnrequestedRates()
    {
        await using var fixture = new CommerceFixture();
        fixture.Db.ShippingZones.Add(new ShippingZone { Name = "Custom zone", Country = "Sri Lanka" });
        await fixture.Db.SaveChangesAsync();
        await Seed(fixture);
        Assert.Single(fixture.Db.ShippingZones);
        Assert.Empty(fixture.Db.ShippingMethods);
    }
}
