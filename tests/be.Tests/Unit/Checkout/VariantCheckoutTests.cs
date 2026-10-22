using be.Application.Features.Cart;
using be.Application.Abstractions.Payments;
using be.Application.Features.CustomDesigns;
using be.Domain.Catalog;
using be.Domain.Common;
using be.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace be.Tests;
public sealed class VariantCheckoutTests
{
    [Fact]
    public async Task DistinctOptionsKeepIndependentCartPricesAndStockThroughPaymentFailure()
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync(stock: 100);
        fixture.Db.CartItems.RemoveRange(fixture.Db.CartItems);
        var first = new ProductVariant { ProductId = 1, Name = "Matte", Sku = "TEST-MATTE", PriceOverride = 150, StockQuantity = 5 };
        var second = new ProductVariant { ProductId = 1, Name = "Gloss", Sku = "TEST-GLOSS", PriceOverride = 200, StockQuantity = 8 };
        fixture.Db.AddRange(first, second);
        await fixture.Db.SaveChangesAsync();
        var cart = new CartService(fixture.Db, new CustomStickerPricingService(), Options.Create(fixture.Settings), new TestRequestActor("a"));
        await cart.AddToCart(new AddToCartRequest { ProductId = 1, ProductVariantId = first.Id, Quantity = 2 }, default);
        await cart.AddToCart(new AddToCartRequest { ProductId = 1, ProductVariantId = second.Id, Quantity = 3 }, default);
        var contents = await cart.GetCart(default);
        Assert.Equal(2, contents.Items.Count);
        Assert.Equal(900m, contents.Total);
        Assert.Contains(contents.Items, x => x.ProductVariantId == first.Id && x.ProductName.Contains("Matte") && x.Subtotal == 300m);
        var request = await fixture.RequestAsync();
        var session = await fixture.Checkout.CreateAsync("a", request, default);
        var order = await fixture.Db.Orders.Include(x => x.OrderItems).SingleAsync();
        Assert.Contains(order.OrderItems, x => x.ProductVariantId == first.Id && x.UnitPrice == 150 && x.SkuSnapshot == "TEST-MATTE");
        Assert.Equal(3, await fixture.Db.ProductVariants.Where(x => x.Id == first.Id).Select(x => x.StockQuantity).SingleAsync());
        Assert.Equal(5, await fixture.Db.ProductVariants.Where(x => x.Id == second.Id).Select(x => x.StockQuantity).SingleAsync());
        Assert.Equal(100, (await fixture.Db.Products.SingleAsync()).Stock);
        var payment = await fixture.Db.Payments.SingleAsync(x => x.Id == session.PaymentId);
        await fixture.Payments.ApplyAsync(fixture.State(payment, GatewayState.Failed), "variant-decline", default);
        Assert.Equal(5, await fixture.Db.ProductVariants.Where(x => x.Id == first.Id).Select(x => x.StockQuantity).SingleAsync());
        Assert.Equal(8, await fixture.Db.ProductVariants.Where(x => x.Id == second.Id).Select(x => x.StockQuantity).SingleAsync());
        Assert.Equal(100, (await fixture.Db.Products.SingleAsync()).Stock);
    }

    [Fact]
    public async Task RejectsCrossProductInactiveAndOversoldOptionsAndRecalculatesChangedPrice()
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync();
        var parent = new Product { Name = "Other", Slug = "other", Price = 1, Stock = 10 };
        var wrong = new ProductVariant { Product = parent, Sku = "TEST-OTHER", Name = "Other", StockQuantity = 10 };
        var inactive = new ProductVariant { ProductId = 1, Sku = "TEST-OFF", Name = "Off", StockQuantity = 10, IsActive = false };
        var active = new ProductVariant { ProductId = 1, Sku = "TEST-ON", Name = "On", StockQuantity = 2, PriceOverride = 120 };
        fixture.Db.AddRange(parent, wrong, inactive, active); await fixture.Db.SaveChangesAsync();
        var cart = new CartService(fixture.Db, new CustomStickerPricingService(), Options.Create(fixture.Settings), new TestRequestActor("a"));
        foreach (var id in new[] { wrong.Id, inactive.Id, int.MaxValue })
            Assert.Equal(404, (await Assert.ThrowsAsync<CommerceException>(() => cart.AddToCart(new AddToCartRequest { ProductId = 1, ProductVariantId = id }, default))).Status);
        await Assert.ThrowsAsync<be.Application.Common.RequestValidationException>(() => cart.AddToCart(new AddToCartRequest { ProductId = 1, ProductVariantId = active.Id, Quantity = 3 }, default));
        await cart.AddToCart(new AddToCartRequest { ProductId = 1, ProductVariantId = active.Id }, default);
        active.PriceOverride = 180; await fixture.Db.SaveChangesAsync();
        var priced = await fixture.Pricing.CalculateAsync("a", await fixture.RequestAsync(), default);
        Assert.Contains(priced.Items, x => x.ProductVariantId == active.Id && x.UnitPrice == 180);
    }
}
