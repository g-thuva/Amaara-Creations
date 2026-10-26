using System.Net.Http.Json;
using System.Text.Json;
using be.Application.Abstractions.Payments;
using be.Application.Features.Checkout;
using be.Application.Features.Payments;
using be.Domain.Common;
using be.Domain.Identity;
using be.Domain.Payments;
using be.Domain.Shipping;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace be.Tests.Fixtures.Payments;

public sealed class PaymentWorkflowFixture : IAsyncDisposable
{
    public AcceptanceApiFactory Factory { get; }
    public string Connection { get; }
    public string Database => new SqlConnectionStringBuilder(Connection).InitialCatalog;
    public string Currency { get; }
    public string CustomerEmail { get; private set; } = "";
    public string AdminEmail { get; private set; } = "";
    public string CustomerId { get; private set; } = "";
    public string AdminId { get; private set; } = "";
    private string password = "";
    private string? adminToken;
    private DateTimeOffset adminVerified;
    private readonly SemaphoreSlim adminLock = new(1);
    private readonly SemaphoreSlim eventLock = new(1);
    private readonly Dictionary<Guid, byte[]> events = new();
    public FakePaymentGateway Gateway { get; } = new() { Provider = PaymentFixtureSafety.Provider, Mode = PaymentFixtureSafety.Mode, HostedBaseUrl = "https://payments.fixture.invalid", StoreTransactions = true };
    public SignedFixtureWebhookParser Parser { get; } = new();

    private PaymentWorkflowFixture(string connection, string currency, string stateRoot)
    {
        PaymentFixtureSafety.CheckConnection(connection); Connection = connection; Currency = currency;
        Factory = new()
        {
            TestSqlConnection = connection,
            TestKeysPath = Path.Combine(stateRoot, Database, "keys"),
            PaymentGateway = Gateway,
            ExtraConfiguration = new Dictionary<string, string?>
            {
                ["Checkout:Currency"] = currency, ["Checkout:AllowDevelopmentShipping"] = "true",
                ["Zahls:Enabled"] = "false", ["Zahls:TestProviderConfigured"] = "false", ["Zahls:ChfSupportedByTestProvider"] = "false",
                ["Datatrans:Enabled"] = "false",
                ["Authentication:RefreshCookieName"] = "amaara_contract_refresh",
                ["Authentication:FrontendBaseUrl"] = PaymentFixtureSafety.Storefront,
                ["PublicSite:BaseUrl"] = PaymentFixtureSafety.Storefront,
                ["Cors:AllowedOrigins:0"] = "http://127.0.0.1:5178", ["Cors:AllowedOrigins:1"] = "http://127.0.0.1:5179",
                ["Cors:AllowedOrigins:2"] = PaymentFixtureSafety.Origin, ["Administration:AllowedOrigins:0"] = "http://127.0.0.1:5179",
                ["Cors:AllowedOrigins:3"] = "http://127.0.0.1:5173",
            },
            ExtraServices = services => services.AddSingleton<IPaymentWebhookParser>(Parser),
        };
    }

    public static async Task<PaymentWorkflowFixture> CreateAsync(string server, string currency, string stateRoot, CancellationToken ct = default)
    {
        var connection = PaymentFixtureSafety.NewConnection(server, currency);
        var master = new SqlConnectionStringBuilder(connection) { InitialCatalog = "master" };
        await using (var sql = new SqlConnection(master.ConnectionString))
        {
            await sql.OpenAsync(ct); using var query = sql.CreateCommand(); query.CommandText = "SELECT DB_ID(@name)";
            query.Parameters.AddWithValue("@name", new SqlConnectionStringBuilder(connection).InitialCatalog);
            if (await query.ExecuteScalarAsync(ct) is not DBNull) throw new InvalidOperationException("Payment fixture refuses an existing database.");
        }
        var lab = new PaymentWorkflowFixture(connection, currency, stateRoot);
        try
        {
            await using (var schema = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options)) await schema.Database.MigrateAsync(ct);
            await lab.Factory.SeedCatalogueAsync(currency, PaymentFixtureSafety.Origin);
            var fixture = JsonSerializer.SerializeToElement(await lab.Factory.CreateBrowserFixtureAsync());
            lab.CustomerEmail = fixture.GetProperty("customerEmail").GetString()!;
            lab.AdminEmail = fixture.GetProperty("adminEmail").GetString()!; lab.password = fixture.GetProperty("password").GetString()!;
            using var scope = lab.Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var customer = (await users.FindByEmailAsync(lab.CustomerEmail))!; customer.Name = "FIXTURE customer"; await users.UpdateAsync(customer); lab.CustomerId = customer.Id;
            var admin = (await users.FindByEmailAsync(lab.AdminEmail))!; admin.Name = "FIXTURE test administrator"; await users.UpdateAsync(admin); lab.AdminId = admin.Id;
            var products = await db.Products.Include(p => p.Variants).OrderBy(p => p.Id).ToListAsync(ct);
            foreach (var item in products) item.Name = "FIXTURE " + currency + " sticker " + item.Id;
            // The first sample is deliberately simple; other samples retain variants.
            db.ProductVariants.RemoveRange(products[0].Variants);
            db.Addresses.Add(new() { UserId = customer.Id, Label = "FIXTURE delivery", RecipientName = "Synthetic test buyer", AddressLine1 = "1 Fictional Test Road", City = "Test City", Country = "Testland", IsDefault = true });
            var zone = new ShippingZone { Name = "FIXTURE test zone", Country = "Testland", RequiresPhone = false };
            db.ShippingZones.Add(zone);
            db.ShippingMethods.Add(new() { ShippingZone = zone, Code = "FIXTURE", Name = "FIXTURE delivery", BasePrice = 25, Currency = currency, IsDevelopmentValue = true });
            await db.SaveChangesAsync(ct);
            return lab;
        }
        catch { await lab.DisposeAsync(); throw; }
    }

    public async Task<HttpResponseMessage> SignInAsync(bool management, CancellationToken ct = default)
    {
        using var client = Factory.CreateClient(new() { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", management ? "http://127.0.0.1:5179" : "http://127.0.0.1:5178");
        var response = await client.PostAsJsonAsync(management ? "/api/v1/auth/staff/login" : "/api/v1/auth/sign-in", new { email = management ? AdminEmail : CustomerEmail, password, authenticatorCode = management ? TotpFixture.Code : null }, ct);
        // Reuse this normal staff proof for lab controls rather than consuming
        // the same one-use authenticator code again after opening the dashboard.
        if (management && response.IsSuccessStatusCode)
        {
            adminToken = JsonSerializer.Deserialize<JsonElement>(await response.Content.ReadAsStringAsync(ct)).GetProperty("token").GetString();
            adminVerified = DateTimeOffset.UtcNow;
        }
        return response;
    }

    public async Task<HttpResponseMessage> AdminActionAsync(string path, object values, CancellationToken ct)
    {
        await adminLock.WaitAsync(ct);
        try
        {
            if (adminToken == null || DateTimeOffset.UtcNow - adminVerified >= TimeSpan.FromMinutes(4))
            {
                using var signed = await SignInAsync(true, ct); signed.EnsureSuccessStatusCode();
            }
            using var client = Factory.CreateClient(new() { HandleCookies = false }); client.DefaultRequestHeaders.Authorization = new("Bearer", adminToken);
            client.DefaultRequestHeaders.Add("Origin", "http://127.0.0.1:5179");
            return await client.PostAsJsonAsync(path, values, ct);
        }
        finally { adminLock.Release(); }
    }

    public static GatewayState Outcome(string value) => value switch { "success" => GatewayState.Paid, "decline" => GatewayState.Failed, "cancel" => GatewayState.Cancelled, "pending" => GatewayState.Pending, _ => throw new CommerceException("FIXTURE_OUTCOME_INVALID", "Choose success, decline, cancel or pending.") };

    public async Task EventAsync(long id, string outcome, Guid eventId, bool webhook, CancellationToken ct = default)
    {
        if (eventId == Guid.Empty) throw new CommerceException("FIXTURE_EVENT_INVALID", "An event identifier is required.");
        await eventLock.WaitAsync(ct);
        try
        {
            using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id && value.Provider == PaymentFixtureSafety.Provider && value.ProviderMode == PaymentFixtureSafety.Mode && value.Order!.UserId == CustomerId, ct) ?? throw new CommerceException("FIXTURE_PAYMENT_NOT_FOUND", "Simulated payment not found.", 404);
            var state = Outcome(outcome); var amount = Money.ToMinor(payment.Amount, payment.Currency);
            var status = new GatewayStatus(payment.ProviderTransactionId!, Gateway.MerchantId, payment.ProviderReference!, payment.Currency, amount, state == GatewayState.Paid ? amount : null, "payment", "FIXTURE_" + outcome, state, "FIXTURE") { GatewayId = payment.ProviderGatewayId, Mode = PaymentFixtureSafety.Mode, InvoiceTest = true };
            var payload = Parser.Payload(eventId, status);
            if (events.TryGetValue(eventId, out var previous) && !previous.SequenceEqual(payload)) throw new CommerceException("IDEMPOTENCY_CONFLICT", "This event identifier was already used with another outcome.", 409);
            var current = await Gateway.StatusAsync(payment.ProviderTransactionId!, ct);
            if (current.State is GatewayState.Paid or GatewayState.Failed or GatewayState.Cancelled && current.State != state)
                throw new CommerceException("FIXTURE_TRANSITION_INVALID", "This attempt already has a final outcome. Retry a failed or cancelled payment from the storefront.", 409);
            Gateway.SetStatus(status);
            if (webhook) await scope.ServiceProvider.GetRequiredService<PaymentWebhookService>().ProcessAsync(PaymentFixtureSafety.Provider, payload, Parser.Sign(payload), ct);
            events[eventId] = payload;
        }
        finally { eventLock.Release(); }
    }

    public async ValueTask DisposeAsync() { await Factory.DisposeAsync(); adminLock.Dispose(); eventLock.Dispose(); }
}
