using be.Api.Controllers;
using be.Application.Features.Cart;
using be.Application.Features.Catalog;
using be.Application.Features.CustomDesigns;
using be.Domain.Catalog;
using be.Domain.Identity;
using be.Domain.Common;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace be.Tests;
public sealed class ChfCatalogTests
{
    [Fact]
    public async Task PublicCatalogOnlyExposesProductsPricedInStoreCurrency()
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync(currency: "CHF");
        fixture.Db.Products.Add(new Product { Id = 2, Name = "Existing LKR product", Slug = "lkr-product", Currency = "LKR", Price = 125, BasePrice = 125, Stock = 10 });
        await fixture.Db.SaveChangesAsync();
        var queries = new ProductQueries(fixture.Db, Options.Create(fixture.Settings));
        var catalog = await queries.GetProducts();
        Assert.Equal("CHF", catalog.Currency);
        Assert.Single(catalog.Products);
        Assert.Equal("CHF", catalog.Products[0].Currency);
        Assert.Equal(404, (await Assert.ThrowsAsync<CommerceException>(() => queries.GetProduct(2))).Status);
    }

    [Fact]
    public async Task ExistingLkrCartIsLabeledAndCannotBeMixedWithChf()
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync();
        fixture.Settings.Currency = "CHF";
        fixture.Db.Products.Add(new Product { Id = 2, Name = "TEST CHF product", Slug = "test-chf-product", Currency = "CHF", Price = 25, BasePrice = 25, Stock = 10 });
        await fixture.Db.SaveChangesAsync();
        var cartService = new CartService(fixture.Db, new CustomStickerPricingService(), Options.Create(fixture.Settings), new TestRequestActor("a"));
        var cart = await cartService.GetCart(default);
        Assert.Equal("LKR", cart.Currency);
        Assert.False(cart.CanCheckout);
        Assert.Equal(200m, cart.Total);
        Assert.Equal(409, (await Assert.ThrowsAsync<CommerceException>(() => cartService.AddToCart(new AddToCartRequest { ProductId = 2, Quantity = 1 }, default))).Status);
        Assert.Single(await fixture.Db.CartItems.ToListAsync());
    }
}
