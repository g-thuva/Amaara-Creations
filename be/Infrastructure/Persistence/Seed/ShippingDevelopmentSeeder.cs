using be.Application.Features.Checkout;
using be.Domain.Shipping;
using be.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Persistence.Seed;

public static class ShippingDevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        var checkout = services.GetRequiredService<IOptions<CheckoutOptions>>().Value;
        if (!environment.IsDevelopment() || checkout.Currency != Money.DefaultCurrency) return;

        var context = services.GetRequiredService<ApplicationDbContext>();
        // Initialize an empty local store once. Never replace configured rates,
        // reactivate disabled methods or attach defaults to a custom zone.
        if (await context.ShippingMethods.AnyAsync(cancellationToken) || await context.ShippingZones.AnyAsync(cancellationToken)) return;

        var zone = new ShippingZone
        {
            Name = "Sri Lanka",
            Country = "Sri Lanka",
            RequiresPhone = false,
        };
        context.ShippingZones.Add(zone);
        context.ShippingMethods.AddRange(
            new ShippingMethod
            {
                ShippingZone = zone,
                Code = "STANDARD",
                Name = "Standard delivery",
                Currency = Money.DefaultCurrency,
                BasePrice = 3.50m,
                FreeShippingThreshold = 50,
                DisplayOrder = 0,
            },
            new ShippingMethod
            {
                ShippingZone = zone,
                Code = "EXPRESS",
                Name = "Express delivery",
                Currency = Money.DefaultCurrency,
                BasePrice = 5.50m,
                DisplayOrder = 1,
            });
        await context.SaveChangesAsync(cancellationToken);
    }
}
