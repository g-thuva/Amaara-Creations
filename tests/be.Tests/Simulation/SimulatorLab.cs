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

namespace be.Tests.Simulation;

public sealed class SimulatorLab : IAsyncDisposable
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
    public FakePaymentGateway Gateway { get; } = new() { Provider = SimulatorSafety.Provider, Mode = SimulatorSafety.Mode, HostedBaseUrl = "https://payments.simulator.invalid", StoreTransactions = true };
    public SimulatorWebhookParser Parser { get; } = new();

    private SimulatorLab(string connection, string currency, string stateRoot)
    {
        SimulatorSafety.CheckConnection(connection); Connection = connection; Currency = currency;
        Factory = new()
        {
            TestSqlConnection = connection,
            TestKeysPath = Path.Combine(stateRoot, Database, "keys"),
            PaymentGateway = Gateway,
            ExtraConfiguration = new Dictionary<string, string?>
            {
                ["Checkout:Currency"] = currency, ["Checkout:AllowDevelopmentShipping"] = "true",
                ["Authentication:RefreshCookieName"] = "amaara_simulator_refresh",
                ["Authentication:FrontendBaseUrl"] = SimulatorSafety.Storefront,
                ["PublicSite:BaseUrl"] = SimulatorSafety.Storefront,
                ["Cors:AllowedOrigins:0"] = "http://127.0.0.1:5188", ["Cors:AllowedOrigins:1"] = "http://127.0.0.1:5189",
                ["Cors:AllowedOrigins:2"] = SimulatorSafety.Origin, ["Administration:AllowedOrigins:0"] = "http://127.0.0.1:5189",
            },
            ExtraServices = services => services.AddSingleton<IPaymentWebhookParser>(Parser),
        };
    }

    public static async Task<SimulatorLab> CreateAsync(string server, string currency, string stateRoot, CancellationToken ct = default)
    {
        var connection = SimulatorSafety.NewConnection(server, currency);
        var master = new SqlConnectionStringBuilder(connection) { InitialCatalog = "master" };
        await using (var sql = new SqlConnection(master.ConnectionString))
        {
            await sql.OpenAsync(ct); using var query = sql.CreateCommand(); query.CommandText = "SELECT DB_ID(@name)";
            query.Parameters.AddWithValue("@name", new SqlConnectionStringBuilder(connection).InitialCatalog);
            if (await query.ExecuteScalarAsync(ct) is not DBNull) throw new InvalidOperationException("Simulator refuses an existing database.");
        }
        var lab = new SimulatorLab(connection, currency, stateRoot);
        try
        {
            await using (var schema = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options)) await schema.Database.MigrateAsync(ct);
            await lab.Factory.SeedCatalogueAsync(currency, SimulatorSafety.Origin);
            var fixture = JsonSerializer.SerializeToElement(await lab.Factory.CreateBrowserFixtureAsync());
            lab.CustomerEmail = fixture.GetProperty("customerEmail").GetString()!;
            lab.AdminEmail = fixture.GetProperty("adminEmail").GetString()!; lab.password = fixture.GetProperty("password").GetString()!;
            using var scope = lab.Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>(); var users = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var customer = (await users.FindByEmailAsync(lab.CustomerEmail))!; customer.Name = "SIMULATED customer"; await users.UpdateAsync(customer); lab.CustomerId = customer.Id;
            var admin = (await users.FindByEmailAsync(lab.AdminEmail))!; admin.Name = "SIMULATED test administrator"; await users.UpdateAsync(admin); lab.AdminId = admin.Id;
            var products = await db.Products.Include(p => p.Variants).OrderBy(p => p.Id).ToListAsync(ct);
            foreach (var item in products) item.Name = "SIMULATED " + currency + " sticker " + item.Id;
            // The first sample is deliberately simple; other samples retain variants.
            db.ProductVariants.RemoveRange(products[0].Variants);
            db.Addresses.Add(new() { UserId = customer.Id, Label = "SIMULATED delivery", RecipientName = "Synthetic test buyer", AddressLine1 = "1 Fictional Test Road", City = "Test City", Country = "Testland", IsDefault = true });
            var zone = new ShippingZone { Name = "SIMULATED test zone", Country = "Testland", RequiresPhone = false };
            db.ShippingZones.Add(zone);
            db.ShippingMethods.Add(new() { ShippingZone = zone, Code = "SIMULATED", Name = "SIMULATED delivery", BasePrice = 25, Currency = currency, IsDevelopmentValue = true });
            await db.SaveChangesAsync(ct);
            await db.Database.ExecuteSqlRawAsync("EXEC sys.sp_addextendedproperty @name=N'AmaaraLocalPaymentSimulator', @value=N'Synthetic disposable data only';", ct);
            return lab;
        }
        catch { await lab.DisposeAsync(); throw; }
    }

    public async Task<HttpResponseMessage> SignInAsync(bool management, CancellationToken ct = default)
    {
        using var client = Factory.CreateClient(new() { HandleCookies = false });
        client.DefaultRequestHeaders.Add("Origin", management ? "http://127.0.0.1:5189" : "http://127.0.0.1:5188");
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
            client.DefaultRequestHeaders.Add("Origin", "http://127.0.0.1:5189");
            return await client.PostAsJsonAsync(path, values, ct);
        }
        finally { adminLock.Release(); }
    }

    public static GatewayState Outcome(string value) => value switch { "success" => GatewayState.Paid, "decline" => GatewayState.Failed, "cancel" => GatewayState.Cancelled, "pending" => GatewayState.Pending, _ => throw new CommerceException("SIMULATOR_OUTCOME_INVALID", "Choose success, decline, cancel or pending.") };

    public async Task EventAsync(long id, string outcome, Guid eventId, bool webhook, CancellationToken ct = default)
    {
        if (eventId == Guid.Empty) throw new CommerceException("SIMULATOR_EVENT_INVALID", "An event identifier is required.");
        await eventLock.WaitAsync(ct);
        try
        {
            using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var payment = await db.Payments.AsNoTracking().SingleOrDefaultAsync(value => value.Id == id && value.Provider == SimulatorSafety.Provider && value.ProviderMode == SimulatorSafety.Mode && value.Order!.UserId == CustomerId, ct) ?? throw new CommerceException("SIMULATOR_PAYMENT_NOT_FOUND", "Simulated payment not found.", 404);
            var state = Outcome(outcome); var amount = Money.ToMinor(payment.Amount, payment.Currency);
            var status = new GatewayStatus(payment.ProviderTransactionId!, Gateway.MerchantId, payment.ProviderReference!, payment.Currency, amount, state == GatewayState.Paid ? amount : null, "payment", "SIMULATED_" + outcome, state, "SIMULATED") { GatewayId = payment.ProviderGatewayId, Mode = SimulatorSafety.Mode, InvoiceTest = true };
            var payload = Parser.Payload(eventId, status);
            if (events.TryGetValue(eventId, out var previous) && !previous.SequenceEqual(payload)) throw new CommerceException("IDEMPOTENCY_CONFLICT", "This event identifier was already used with another outcome.", 409);
            Gateway.SetStatus(status);
            if (webhook) await scope.ServiceProvider.GetRequiredService<PaymentWebhookService>().ProcessAsync(SimulatorSafety.Provider, payload, Parser.Sign(payload), ct);
            events[eventId] = payload;
        }
        finally { eventLock.Release(); }
    }

    public async Task<object> InspectAsync(CancellationToken ct = default)
    {
        using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return new
        {
            simulated = true, database = Database, currency = Currency,
            payments = await db.Payments.AsNoTracking().OrderByDescending(p => p.Id).Select(p => new { p.Id, p.OrderId, p.Order!.OrderNumber, p.Provider, p.ProviderMode, Status = p.Status.ToString(), OrderStatus = p.Order.Status.ToString(), p.Amount, p.Currency, p.SettledAmount, p.RefundedAmount, p.ProviderTransactionId, p.ProviderStatus }).ToListAsync(ct),
            attempts = await db.CheckoutAttempts.AsNoTracking().OrderBy(a => a.Id).Select(a => new { a.Id, a.OrderId, a.PaymentId, a.Provider }).ToListAsync(ct),
            reservations = await db.InventoryReservations.AsNoTracking().Select(r => new { r.OrderId, r.ProductId, r.Quantity, r.Status }).ToListAsync(ct),
            refunds = await db.Refunds.AsNoTracking().Select(r => new { r.Id, r.PaymentId, r.Amount, r.Currency, r.Status, r.IdempotencyKey, r.RequestedByUserId }).ToListAsync(ct),
            inventory = await db.Products.AsNoTracking().Select(p => new { p.Id, p.Name, p.Stock, p.Currency }).ToListAsync(ct),
            events = await db.PaymentWebhookEvents.AsNoTracking().Select(e => new { e.PaymentId, e.ProviderStatus, e.ProcessedAt }).ToListAsync(ct),
        };
    }

    public async ValueTask DisposeAsync() { await Factory.DisposeAsync(); adminLock.Dispose(); eventLock.Dispose(); }
}
