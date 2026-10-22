using be.Application.Abstractions.Email;
using be.Application.Abstractions.Payments;
using be.Domain.Catalog;
using be.Domain.Cms;
using be.Domain.CustomDesigns;
using be.Domain.Identity;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Domain.Reviews;
using be.Infrastructure.Persistence;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.RateLimiting;
using Microsoft.Data.SqlClient;

namespace be.Tests;
// Referenced only by the acceptance executable. Never registered by the application.
public sealed class AcceptanceApiFactory : WebApplicationFactory<global::Program>
{
    private readonly string signingKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
    private readonly string databaseName = "Acceptance-" + Guid.NewGuid();
    public CapturedAcceptanceEmail Email { get; } = new();
    public TimeProvider Clock { get; init; } = TimeProvider.System;
    public bool RequestsEnabled { get; init; } = true;
    public bool EmailChangeEnabled { get; init; } = true;
    public bool AdministrationEnabled { get; init; } = true;
    public string? TestSqlConnection { get; init; }
    public string? TestKeysPath { get; init; }
    public string? TestSigningKey { get; init; }

    private readonly FakePaymentGateway gateway = new();
    private readonly string storageRoot = Path.Combine(Path.GetTempPath(), "AmaaraAcceptance", Guid.NewGuid().ToString("N"));
    private Dictionary<string, string?> Configuration => new()
    {
        ["ConnectionStrings:DefaultConnection"] = TestSqlConnection ?? "Server=test-only;Database=Acceptance;Integrated Security=true;Encrypt=false",
        ["DataProtection:KeysPath"] = TestKeysPath ?? Path.Combine(storageRoot, "DataProtectionKeys"),
        ["Jwt:Key"] = TestSigningKey ?? signingKey,
        ["Jwt:Issuer"] = "Acceptance",
        ["Jwt:Audience"] = "Acceptance",
        ["Cors:AllowedOrigins:0"] = "http://127.0.0.1:5178",
        ["Cors:AllowedOrigins:1"] = "http://127.0.0.1:5179",
        ["Administration:Enabled"] = AdministrationEnabled.ToString(),
        ["Administration:AllowedOrigins:0"] = "http://127.0.0.1:5179",
        ["CustomerRequests:QuotesEnabled"] = RequestsEnabled.ToString(),
        ["Privacy:RequestsEnabled"] = RequestsEnabled.ToString(),
        ["Authentication:EmailChangeEnabled"] = EmailChangeEnabled.ToString(),
        ["AdminBootstrap:Enabled"] = "false",
        ["Authentication:FrontendBaseUrl"] = "http://127.0.0.1:5178/Amaara-Creations",
        ["Authentication:RateLimitPermitLimit"] = "200",
        ["Datatrans:Enabled"] = "false",
        ["PublicSite:BaseUrl"] = "https://store.example.test/Amaara-Creations"
    };

    protected override IHost CreateHost(IHostBuilder builder)
    {
        if (TestSqlConnection != null && !new SqlConnectionStringBuilder(TestSqlConnection).InitialCatalog.StartsWith("AmaaraPhase6Tests_", StringComparison.Ordinal)) throw new InvalidOperationException("Acceptance SQL must use an explicitly disposable test database.");
        Directory.CreateDirectory(storageRoot);
        builder.ConfigureHostConfiguration(c => c.AddInMemoryCollection(Configuration));
        return base.CreateHost(builder);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development").UseContentRoot(storageRoot);
        builder.ConfigureAppConfiguration((_, c) => c.AddInMemoryCollection(Configuration));
        builder.ConfigureLogging(c => c.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton(Clock);
            // This shared disposable host runs many upload cases in one minute.
            // Keep finite limits in the fixture without changing production limits.
            services.RemoveAll<IConfigureOptions<RateLimiterOptions>>();
            services.AddRateLimiter(options =>
            {
                options.RejectionStatusCode = 429;
                foreach (var name in new[]
                {
                    "auth-sensitive",
                    "auth-refresh",
                    "custom-quote",
                    "custom-upload"
                }

                )
                    options.AddFixedWindowLimiter(name, limiter =>
                    {
                        limiter.PermitLimit = 1000;
                        limiter.Window = TimeSpan.FromMinutes(1);
                        limiter.QueueLimit = 0;
                    });
            });
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();
            services.AddSingleton(TestSqlConnection == null ? new DbContextOptionsBuilder<ApplicationDbContext>().UseInMemoryDatabase(databaseName).Options : new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(TestSqlConnection).Options);
            services.AddScoped<ApplicationDbContext>();
            services.RemoveAll<IHostedService>();
            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Email);
            services.RemoveAll<IPaymentGateway>();
            services.AddSingleton<IPaymentGateway>(gateway);
        });
    }

    public async Task SeedCatalogueAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var category = new Category
        {
            Name = "Acceptance fixtures",
            Slug = "acceptance-fixtures"
        };
        db.Categories.Add(category);
        for (var i = 1; i <= 13; i++)
        {
            var product = new Product
            {
                Name = $"Storefront QA print {i:00}",
                Slug = $"qa-print-{i:00}",
                CategoryEntity = category,
                Price = 100 + i,
                BasePrice = 100 + i,
                Stock = 100,
                Description = "Disposable acceptance fixture.",
                ImageUrl = "http://127.0.0.1:5198/__fixture/image/first.png"
            };
            product.Media.Add(new ProductMedia { StorageKey = "fixture-first", Url = product.ImageUrl, IsPrimary = true, SortOrder = 0 });
            product.Media.Add(new ProductMedia { StorageKey = "fixture-second", Url = "http://127.0.0.1:5198/__fixture/image/second.png", SortOrder = 1 });
            product.Variants.Add(new ProductVariant { Sku = $"QA-{i}", Name = "Large format", PriceOverride = 250, StockQuantity = 100 });
            db.Products.Add(product);
        }

        var page = new CmsPage
        {
            Title = "Acceptance CMS",
            Slug = "acceptance-cms",
            IsPublished = true,
            PublishedAt = DateTime.UtcNow
        };
        page.Sections.Add(new CmsSection { SectionKey = "intro", ContentType = "text", Content = "Published content from the real CMS API." });
        page.Sections.Add(new CmsSection { SectionKey = "faq", ContentType = "json", Content = "{\"type\":\"faq\",\"items\":[{\"question\":\"Is this real CMS content?\",\"answer\":\"Yes, this is a temporary verification page.\"}]}" });
        db.CmsPages.Add(page);
        await db.SaveChangesAsync();
    }

    public async Task<object> CreateBrowserFixtureAsync(string? refundOutcome = null)
    {
        if (refundOutcome != null && refundOutcome is not ("Completed" or "Pending" or "Failed"))
            throw new ArgumentException("Unknown test refund outcome.");
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        var password = "Qa4!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20));
        async Task<string> Account(string role)
        {
            var email = $"acceptance-{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.test";
            var user = new User
            {
                UserName = email,
                Email = email,
                Name = "Disposable acceptance account",
                EmailConfirmed = true
            };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded || !(await users.AddToRoleAsync(user, role)).Succeeded)
                throw new InvalidOperationException("Acceptance account setup failed.");
            if (role != "Customer")
            {
                await users.SetAuthenticationTokenAsync(user, "[AspNetUserStore]", "AuthenticatorKey", TotpFixture.Secret);
                await users.SetTwoFactorEnabledAsync(user, true);
            }
            return email;
        }

        var customerEmail = await Account("Customer");
        var adminEmail = await Account("Admin");
        Order? refundOrder = null;
        Payment? refundPayment = null;
        if (refundOutcome != null)
        {
            // This paid snapshot exists only in the disposable acceptance DB.
            // Browser refunds still execute the real controller/service/DTO flow.
            gateway.RejectRefund = refundOutcome == "Failed";
            gateway.AmbiguousRefund = refundOutcome == "Pending";
            gateway.NextStatus = null;
            var customer = await users.FindByEmailAsync(customerEmail);
            refundOrder = new Order
            {
                UserId = customer!.Id,
                OrderNumber = "REFUND-FIXTURE-" + Guid.NewGuid().ToString("N"),
                Status = OrderStatus.Confirmed,
                FinalizedAt = DateTime.UtcNow,
                Currency = "LKR",
                Subtotal = 200,
                ShippingTotal = 25,
                Total = 225,
                ShippingMethodName = "Test-only delivery snapshot"
            };
            refundOrder.OrderItems.Add(new OrderItem { ProductId = await db.Products.MinAsync(p => p.Id), ProductNameSnapshot = "Test-only refund item", Quantity = 2, Price = 100, Subtotal = 200 });
            refundPayment = new Payment
            {
                Order = refundOrder,
                Provider = gateway.Provider,
                Status = PaymentStatus.Paid,
                Amount = 225,
                AuthorizedAmount = 225,
                SettledAmount = 225,
                Currency = "LKR",
                PaidAt = DateTime.UtcNow,
                ProviderReference = "TEST-REFUND-" + Guid.NewGuid().ToString("N"),
                ProviderTransactionId = Random.Shared.NextInt64(100000000000000000, 900000000000000000).ToString()
            };
            db.AddRange(refundOrder, refundPayment);
            await db.SaveChangesAsync();
        }

        return new
        {
            products = await db.Products.OrderBy(p => p.Id).Select(p => p.Id).ToArrayAsync(),
            category = await db.Categories.Select(c => c.Id).SingleAsync(),
            cmsSlug = "acceptance-cms",
            customerEmail,
            adminEmail,
            password,
            orderId = refundOrder?.Id,
            paymentId = refundPayment?.Id
        };
    }

    public async Task<object> CreateOperationsFixtureAsync()
    {
        var basic = await CreateBrowserFixtureAsync("Completed");
        var identity = System.Text.Json.JsonSerializer.SerializeToElement(basic);
        var password = identity.GetProperty("password").GetString()!;
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
        async Task<string> Staff(string role)
        {
            var email = $"operations-{role.ToLowerInvariant()}-{Guid.NewGuid():N}@example.test";
            var account = new User
            {
                Email = email,
                UserName = email,
                Name = "Disposable " + role,
                EmailConfirmed = true
            };
            if (!(await users.CreateAsync(account, password)).Succeeded || !(await users.AddToRoleAsync(account, role)).Succeeded)
                throw new InvalidOperationException("Operations fixture setup failed.");
            await users.SetAuthenticationTokenAsync(account, "[AspNetUserStore]", "AuthenticatorKey", TotpFixture.Secret);
            await users.SetTwoFactorEnabledAsync(account, true);
            return email;
        }

        var supportEmail = await Staff("CustomerSupport");
        var analystEmail = await Staff("Analyst");
        var marketingEmail = await Staff("Marketing");
        var superAdminEmail = await Staff("SuperAdmin");
        var customer = await users.FindByEmailAsync(identity.GetProperty("customerEmail").GetString()!);
        var orderId = identity.GetProperty("orderId").GetInt32();
        var order = await db.Orders.Include(o => o.OrderItems).SingleAsync(o => o.Id == orderId);
        var configuration = await db.CustomBuilderConfigurationVersions.OrderBy(v => v.Id).FirstAsync();
        var design = new CustomDesign
        {
            UserId = customer!.Id,
            Name = "TEST ordered proof job " + orderId,
            Status = CustomDesignStatus.Ordered,
            Width = 5,
            Height = 5,
            Quantity = 1,
            MaterialCode = "standard",
            FinishCode = "standard",
            ShapeCode = "rectangle",
            UnitPrice = 25,
            CalculatedPrice = 25,
            BuilderConfigurationVersionId = configuration.Id,
            ProofStatus = ProofStatus.Preparing,
            ProductionStatus = ProductionStatus.NotStarted,
            PricingRuleVersion = "test-only-v1"
        };
        db.Add(design);
        await db.SaveChangesAsync();
        order.OrderItems.Add(new OrderItem { ItemType = OrderItemType.CustomDesign, CustomDesignId = design.Id, ProductNameSnapshot = design.Name, Quantity = 1, UnitPrice = 25, Price = 25, Subtotal = 25, ConfigurationSnapshotJson = "{\"schemaVersion\":1,\"width\":5,\"height\":5,\"quantity\":1,\"currency\":\"LKR\",\"proofRequired\":true,\"materialCode\":\"standard\",\"finishCode\":\"standard\",\"shapeCode\":\"rectangle\",\"pricingVersion\":\"test-only-v1\"}" });
        order.Subtotal = 225;
        order.Total = 250;
        var payment = await db.Payments.SingleAsync(p => p.OrderId == orderId);
        payment.Amount = 250;
        payment.AuthorizedAmount = 250;
        payment.SettledAmount = 250;
        // The fixture is intentionally synthetic and uses no provider or business data.
        await db.SaveChangesAsync();
        var review = new Review
        {
            UserId = customer.Id,
            ProductId = identity.GetProperty("products")[0].GetInt32(),
            Rating = 5,
            Comment = "TEST pending moderation " + orderId
        };
        db.Add(review);
        await db.SaveChangesAsync();
        return new
        {
            basic,
            customerId = customer.Id,
            supportEmail,
            analystEmail,
            marketingEmail,
            superAdminEmail,
            designId = design.Id,
            reviewId = review.Id
        };
    }
}

public sealed class CapturedAcceptanceEmail : IEmailService
{
    public ConcurrentQueue<(string UserId, string Subject, string Body)> SecurityNotifications { get; } = new();
    private readonly ConcurrentDictionary<string, string> confirmations = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, string> emailChanges = new(StringComparer.OrdinalIgnoreCase);
    public IEmailService? Delivery { get; set; }

    public string? Confirmation(string email) => confirmations.TryGetValue(email, out var url) ? url : null;
    public string? EmailChangeToken(string email) => emailChanges.TryGetValue(email, out var token) ? token : null;
    public async Task SendEmailConfirmationAsync(User user, string url, CancellationToken ct = default)
    {
        if (Delivery != null)
            await Delivery.SendEmailConfirmationAsync(user, url, ct);
        confirmations[user.Email!] = url;
    }

    public Task SendPasswordResetAsync(User user, string url, CancellationToken ct = default) => Delivery?.SendPasswordResetAsync(user, url, ct) ?? Task.CompletedTask;
    public async Task SendTransactionAsync(User user, string subject, string body, CancellationToken ct = default)
    {
        if (Delivery != null) await Delivery.SendTransactionAsync(user, subject, body, ct);
        if (subject.Contains("authenticator", StringComparison.OrdinalIgnoreCase)) SecurityNotifications.Enqueue((user.Id, subject, body));
        if (subject == "Confirm your sign-in email change") emailChanges[user.Email!] = body.Split("\n\n")[1];
    }
}
