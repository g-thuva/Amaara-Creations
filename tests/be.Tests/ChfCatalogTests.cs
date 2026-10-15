using System.Security.Claims;
using be.Controllers;
using be.DTOs.Cart;
using be.DTOs.Product;
using be.Models;
using be.Services;
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

        var controller = new ProductsController(fixture.Db, NullLogger<ProductsController>.Instance, Options.Create(fixture.Settings));
        var result = await controller.GetProducts();
        var catalog = Assert.IsType<ProductListResponse>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.Equal("CHF", catalog.Currency);
        Assert.Single(catalog.Products);
        Assert.Equal("CHF", catalog.Products[0].Currency);
        Assert.IsType<NotFoundObjectResult>((await controller.GetProduct(2)).Result);
    }

    [Fact]
    public async Task ExistingLkrCartIsLabeledAndCannotBeMixedWithChf()
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync();
        fixture.Settings.Currency = "CHF";
        fixture.Db.Products.Add(new Product { Id = 2, Name = "TEST CHF product", Slug = "test-chf-product", Currency = "CHF", Price = 25, BasePrice = 25, Stock = 10 });
        await fixture.Db.SaveChangesAsync();

        var controller = new CartController(fixture.Db, new CustomStickerPricingService(), Options.Create(fixture.Settings))
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, "a")], "test"))
            } }
        };
        var cart = Assert.IsType<CartResponse>(Assert.IsType<OkObjectResult>((await controller.GetCart(default)).Result).Value);
        Assert.Equal("LKR", cart.Currency);
        Assert.False(cart.CanCheckout);
        Assert.Equal(200m, cart.Total);
        Assert.IsType<ConflictObjectResult>((await controller.AddToCart(new AddToCartRequest { ProductId = 2, Quantity = 1 }, default)).Result);
        Assert.Single(await fixture.Db.CartItems.ToListAsync());
    }
}
