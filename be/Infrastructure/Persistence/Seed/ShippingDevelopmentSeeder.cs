using be.Application.Features.Checkout;
using be.Domain.Shipping;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Persistence.Seed;

public static class ShippingDevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var environment = services.GetRequiredService<IHostEnvironment>();
        var checkout = services.GetRequiredService<IOptions<CheckoutOptions>>().Value;
        if (!environment.IsDevelopment() || checkout.Currency != "LKR") return;

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
                Currency = "LKR",
                BasePrice = 350,
                FreeShippingThreshold = 5000,
                DisplayOrder = 0,
            },
            new ShippingMethod
            {
                ShippingZone = zone,
                Code = "EXPRESS",
                Name = "Express delivery",
                Currency = "LKR",
                BasePrice = 550,
                DisplayOrder = 1,
            });
        await context.SaveChangesAsync(cancellationToken);
    }
}
