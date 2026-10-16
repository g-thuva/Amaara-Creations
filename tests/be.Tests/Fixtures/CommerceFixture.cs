using be.Domain.Common;
using be.Application.Abstractions.Payments;
using be.Application.Features.Checkout;
using be.Application.Features.CustomDesigns;
using be.Application.Features.Inventory;
using be.Application.Features.Orders;
using be.Application.Features.Payments;
using be.Application.Features.Promotions;
using be.Domain.Cart;
using be.Domain.Catalog;
using be.Domain.CustomDesigns;
using be.Domain.Identity;
using be.Domain.Payments;
using be.Domain.Shipping;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace be.Tests;
internal sealed class CommerceFixture : IAsyncDisposable
{
    public ApplicationDbContext Db { get; }
    public FakePaymentGateway Gateway { get; } = new();
    public CheckoutOptions Settings { get; } = new();
    public CheckoutPricingService Pricing { get; }
    public InventoryReservationService Inventory { get; }
    public CheckoutService Checkout { get; }
    public PaymentReconciliationService Payments { get; }
    public RefundService Refunds { get; }
    public OrderLifecycleService Lifecycle { get; }

    public CommerceFixture(ApplicationDbContext? db = null, FakePaymentGateway? gateway = null)
    {
        Db = db ?? new(new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        if (gateway != null)
            Gateway = gateway;
        var options = Options.Create(Settings);
        var promotions = new PromotionService(Db);
        Pricing = new(Db, new CustomStickerPricingService(), promotions, Gateway, options, new TestEnvironment());
        Inventory = new(Db, options);
        Checkout = new(Db, Pricing, Inventory, promotions, Gateway, options, NullLogger<CheckoutService>.Instance);
        Payments = new(Db, Gateway, Inventory, options, NullLogger<PaymentReconciliationService>.Instance);
        Refunds = new(Db, Gateway);
        Lifecycle = new(Db, Inventory);
    }

    public async Task SeedAsync(int stock = 10, bool custom = false, string currency = "LKR")
    {
        Settings.Currency = currency;
        Db.AddRange(new User { Id = "a", Name = "Customer", UserName = "a@example.test", Email = "a@example.test" }, new User { Id = "b", Name = "Other", UserName = "b@example.test", Email = "b@example.test" }, new Address { Id = 1, UserId = "a", RecipientName = "Customer", AddressLine1 = "Example street", City = "Colombo", Country = "Sri Lanka", Phone = "+94770000000" }, new Address { Id = 2, UserId = "b", RecipientName = "Other", AddressLine1 = "Other street", City = "Colombo", Country = "Sri Lanka", Phone = "+94770000000" }, new ShippingZone { Id = 1, Name = "Test zone", Country = "Sri Lanka", RequiresPhone = true }, new ShippingMethod { Id = 1, ShippingZoneId = 1, Code = "TEST", Name = "Test delivery", BasePrice = 25, Currency = currency }, new Product { Id = 1, Name = "Sticker", Slug = "sticker", Price = 100, BasePrice = 100, Currency = currency, Stock = stock }, new CartItem { Id = 1, UserId = "a", ProductId = 1, Quantity = 2 });
        if (custom)
        {
            var config = new CustomBuilderConfigurationVersion
            {
                Id = 1,
                VersionNumber = 1,
                Status = BuilderVersionStatus.Published,
                Currency = currency,
                MinimumWidth = 1,
                MaximumWidth = 30,
                WidthStep = 1,
                MinimumHeight = 1,
                MaximumHeight = 30,
                HeightStep = 1,
                MinimumQuantity = 1,
                MaximumQuantity = 100,
                QuantityStep = 1,
                PricePerSquareUnit = 2,
                ProofRequired = true
            };
            var design = new CustomDesign
            {
                Id = 1,
                UserId = "a",
                Name = "Custom stickers",
                Width = 2,
                Height = 3,
                Quantity = 4,
                BuilderConfigurationVersion = config,
                Status = CustomDesignStatus.InCart
            };
            Db.AddRange(config, design, new CartItem { Id = 2, UserId = "a", ItemType = CartItemType.CustomDesign, CustomDesign = design, Quantity = 4 });
        }

        await Db.SaveChangesAsync();
    }

    public async Task<be.Application.Features.Checkout.CheckoutSessionRequest> RequestAsync(string? coupon = null)
    {
        var request = new be.Application.Features.Checkout.CheckoutSessionRequest
        {
            AddressId = 1,
            ShippingMethodId = 1,
            CouponCode = coupon,
            IdempotencyKey = Guid.NewGuid()
        };
        request.QuoteVersion = (await Pricing.CalculateAsync("a", request, default)).Quote.QuoteVersion;
        return request;
    }

    public async Task<Payment> StartAsync()
    {
        var session = await Checkout.CreateAsync("a", await RequestAsync(), default);
        return await Db.Payments.SingleAsync(p => p.Id == session.PaymentId);
    }

    public GatewayStatus State(Payment p, GatewayState state = GatewayState.Paid) => new(p.ProviderTransactionId!, Gateway.MerchantId, p.ProviderReference!, p.Currency, Money.ToMinor(p.Amount, p.Currency), state == GatewayState.Paid ? Money.ToMinor(p.Amount, p.Currency) : null, "payment", state switch
    {
        GatewayState.Paid => "settled",
        GatewayState.Failed => "failed",
        GatewayState.Cancelled => "canceled",
        GatewayState.Authorized => "authorized",
        _ => "initialized"
    }, state, "VIS");
    public ValueTask DisposeAsync() => Db.DisposeAsync();
}

internal sealed class FakePaymentGateway : IPaymentGateway
{
    private long counter = Random.Shared.NextInt64(100000000000000000, 900000000000000000);
    public string Provider { get; set; } = "FakeTestGateway";
    public string Mode { get; set; } = "Sandbox";
    public string MerchantId => "1000000000";
    public int Initializations { get; private set; }
    public int RefundCalls { get; private set; }
    public bool FailInitialization { get; set; }
    public bool RejectInitialization { get; set; }
    public bool Available { get; set; } = true;
    public bool RejectRefund { get; set; }
    public bool AmbiguousRefund { get; set; }
    public GatewayStatus? NextStatus { get; set; }

    public void EnsureAvailable(string currency)
    {
        if (!Available)
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Test payment is unavailable.", 503);
    }

    public string RedirectUrl(string id) => Provider == "Zahls" ? "https://zahls.ch/fixture/" + id // Synthetic path on an allowed host; no network request is made.
 : "https://pay.sandbox.datatrans.com/v1/start/" + id;
    public Task<GatewayInitialization> InitializeAsync(GatewayPaymentRequest request, CancellationToken ct)
    {
        Initializations++;
        if (RejectInitialization)
            throw new CommerceException("PAYMENT_PROVIDER_REJECTED", "Test rejection", 502);
        if (FailInitialization)
            throw new HttpRequestException("Test network failure");
        var id = Interlocked.Increment(ref counter).ToString();
        return Task.FromResult(new GatewayInitialization(id, RedirectUrl(id), id));
    }

    public Task<GatewayStatus> StatusAsync(string id, CancellationToken ct) => Task.FromResult(NextStatus ?? throw new HttpRequestException("Test status unavailable"));
    public Task CancelAsync(Payment payment, Guid key, CancellationToken ct) => Task.CompletedTask;
    public Task SettleAsync(Payment payment, Guid key, CancellationToken ct) => Task.CompletedTask;
    public Task<string> RefundAsync(Payment payment, Refund refund, CancellationToken ct)
    {
        RefundCalls++;
        if (RejectRefund)
            throw new CommerceException("PAYMENT_PROVIDER_REJECTED", "Test decline", 502);
        if (AmbiguousRefund)
            throw new HttpRequestException("Test timeout");
        var id = Interlocked.Increment(ref counter).ToString();
        NextStatus = new(id, MerchantId, refund.ProviderReference, refund.Currency, Money.ToMinor(refund.Amount, refund.Currency), null, "credit", "settled", GatewayState.Paid, null);
        return Task.FromResult(id);
    }
}

internal sealed class TestEnvironment : IWebHostEnvironment
{
    public string EnvironmentName { get; set; } = "Development";
    public string ApplicationName { get; set; } = "Tests";
    public string WebRootPath { get; set; } = "";
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = "";
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}
