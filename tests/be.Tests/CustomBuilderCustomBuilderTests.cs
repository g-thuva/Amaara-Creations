using System.Security.Claims;
using System.Text.Json;
using be.Controllers;
using be.Data;
using be.DTOs.CustomBuilder;
using be.DTOs.Order;
using be.Models;
using be.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace be.Tests;

public class CustomBuilderCustomBuilderTests
{
    [Fact]
    public void PricingIsServerCalculatedWithDecimalPrecision()
    {
        var configuration = Configuration();
        var service = new CustomStickerPricingService();

        var quote = service.Quote(configuration, new CustomQuoteRequest
        {
            Width = 10m, Height = 5m, Quantity = 2, MaterialCode = "premium", TextAlignment = "center"
        });

        Assert.Equal(52m, quote.UnitPrice);
        Assert.Equal(104m, quote.Subtotal);
        Assert.Equal("config-v1", quote.PricingVersion);
    }

    [Fact]
    public void PricingRejectsOutOfRangeAndUnknownOptions()
    {
        var service = new CustomStickerPricingService();
        var exception = Assert.Throws<CustomBuilderValidationException>(() => service.Quote(Configuration(), new CustomQuoteRequest
        {
            Width = 1m, Height = 5m, Quantity = 2, MaterialCode = "customer-supplied-id", TextAlignment = "center"
        }));
        Assert.Contains(exception.Errors, error => error.Contains("Width"));
        Assert.Contains(exception.Errors, error => error.Contains("material"));
    }

    [Fact]
    public async Task CustomerDesignEndpointsHideAnotherCustomersDesignAndAssets()
    {
        await using var context = Context();
        context.Users.AddRange(User("customer-a"), User("customer-b"));
        var other = new CustomDesign { Id = 42, UserId = "customer-b", Name = "Private", DesignJson = "{}", Width = 10, Height = 5, Quantity = 2 };
        context.CustomDesigns.Add(other);
        context.CustomDesignAssets.Add(new CustomDesignAsset { Id = 9, CustomDesign = other, StorageKey = "private/custom-artwork/x.png", AssetType = "artwork" });
        await context.SaveChangesAsync();
        var controller = CustomerController(context, "customer-a");

        Assert.IsType<NotFoundResult>((await controller.Get(42, default)).Result);
        Assert.IsType<NotFoundResult>((await controller.Update(42, new SaveCustomDesignRequest(), default)).Result);
        Assert.IsType<NotFoundResult>(await controller.Archive(42, default));
        Assert.IsType<NotFoundResult>(await controller.AddToCart(42, default));
        Assert.IsType<NotFoundResult>((await controller.ApproveProof(42, new ProofResponseRequest { RevisionNumber = 1 }, default)).Result);
        Assert.IsType<NotFoundResult>(await controller.DownloadAsset(42, 9, default));
    }

    [Fact]
    public async Task StaleProofRevisionCannotBeApproved()
    {
        await using var context = Context();
        context.Users.Add(User("customer-a"));
        var design = new CustomDesign { Id = 7, UserId = "customer-a", Name = "Proof design", DesignJson = "{}", Width = 10, Height = 5, Quantity = 2, Status = CustomDesignStatus.Ordered };
        var firstAsset = new CustomDesignAsset { Id = 1, CustomDesign = design, StorageKey = "private/proof-1.png", AssetType = "proof" };
        var secondAsset = new CustomDesignAsset { Id = 2, CustomDesign = design, StorageKey = "private/proof-2.png", AssetType = "proof" };
        context.AddRange(design, firstAsset, secondAsset,
            new CustomDesignProofRevision { CustomDesign = design, Asset = firstAsset, RevisionNumber = 1, Status = ProofRevisionStatus.Superseded },
            new CustomDesignProofRevision { CustomDesign = design, Asset = secondAsset, RevisionNumber = 2, Status = ProofRevisionStatus.AwaitingApproval });
        await context.SaveChangesAsync();
        var controller = CustomerController(context, "customer-a");

        var result = await controller.ApproveProof(7, new ProofResponseRequest { RevisionNumber = 1 }, default);

        Assert.IsType<ConflictObjectResult>(result.Result);
    }

    [Fact]
    public async Task OrderSnapshotRemainsUnchangedAfterPricingConfigurationChanges()
    {
        await using var context = Context();
        var user = User("customer-a");
        var configuration = Configuration();
        var design = new CustomDesign
        {
            Id = 11, UserId = user.Id, User = user, Name = "Wedding labels", DesignJson = "{}", Width = 10, Height = 5, Quantity = 2,
            MaterialCode = "premium", TextAlignment = "center", Status = CustomDesignStatus.InCart, BuilderConfigurationVersion = configuration
        };
        context.AddRange(user, configuration, design, new CartItem
        {
            UserId = user.Id, ItemType = CartItemType.CustomDesign, CustomDesign = design, Quantity = 2, UnitPriceSnapshot = 52m, BuilderConfigurationVersionId = configuration.Id
        });
        await context.SaveChangesAsync();
        var controller = new OrdersController(context, NullLogger<OrdersController>.Instance, new CustomStickerPricingService()) { ControllerContext = ContextFor("customer-a") };

        var action = await controller.CreateOrder(new CreateOrderRequest { ShippingAddress = "Test", ShippingCity = "Colombo", ShippingCountry = "Sri Lanka" });
        var created = Assert.IsType<CreatedAtActionResult>(action.Result);
        var response = Assert.IsType<OrderResponse>(created.Value);
        var snapshotJson = Assert.Single(response.OrderItems).ConfigurationSnapshotJson;
        configuration.PricePerSquareUnit = 99m;
        await context.SaveChangesAsync();

        using var snapshot = JsonDocument.Parse(snapshotJson!);
        Assert.Equal(1, snapshot.RootElement.GetProperty("ConfigurationVersion").GetInt32());
        Assert.Equal(104m, snapshot.RootElement.GetProperty("Subtotal").GetDecimal());
        Assert.Equal(snapshotJson, (await context.OrderItems.SingleAsync()).ConfigurationSnapshotJson);
    }

    private static CustomBuilderConfigurationVersion Configuration()
    {
        var configuration = new CustomBuilderConfigurationVersion
        {
            Id = 1, VersionNumber = 1, Status = BuilderVersionStatus.Published, MeasurementUnit = "cm", MinimumWidth = 2, MaximumWidth = 30, WidthStep = 1,
            MinimumHeight = 2, MaximumHeight = 30, HeightStep = 1, MinimumQuantity = 1, MaximumQuantity = 100, QuantityStep = 1,
            PricePerSquareUnit = 1m, MinimumLinePrice = 0m, Currency = "LKR", PublishedAt = DateTime.UtcNow
        };
        configuration.Options.Add(new CustomBuilderOption { ConfigurationVersion = configuration, Group = BuilderOptionGroup.Material, Code = "premium", Label = "Premium", UnitPriceAdjustment = 2m, IsActive = true });
        return configuration;
    }

    private static ApplicationDbContext Context() => new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
    private static User User(string id) => new() { Id = id, UserName = $"{id}@example.test", Email = $"{id}@example.test", Name = id, EmailConfirmed = true };
    private static ControllerContext ContextFor(string userId) => new() { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, userId)], "test")) } };
    private static CustomDesignsController CustomerController(ApplicationDbContext context, string userId) => new(context, new CustomStickerPricingService(), new FakeStorage()) { ControllerContext = ContextFor(userId) };

    private sealed class FakeStorage : IMediaStorageService
    {
        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public string GetPublicUrl(string storageKey) => throw new InvalidOperationException();
        public Task<Stream> OpenReadAsync(string storageKey, CancellationToken cancellationToken = default) => Task.FromResult<Stream>(new MemoryStream([1]));
        public Task<StoredMedia> UploadAsync(IFormFile file, string area, CancellationToken cancellationToken = default) => throw new NotImplementedException();
        public Task<StoredMedia> UploadPrivateAsync(IFormFile file, string area, CancellationToken cancellationToken = default) => throw new NotImplementedException();
    }
}
