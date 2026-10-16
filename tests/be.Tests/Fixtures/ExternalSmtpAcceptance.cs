using be.Domain.Common;
using be.Application.Abstractions.Email;
using be.Application.Features.Checkout;
using be.Application.Features.Notifications;
using be.Domain.Cart;
using be.Domain.Catalog;
using be.Domain.Identity;
using be.Domain.Payments;
using be.Domain.Shipping;
using be.Infrastructure.BackgroundJobs;
using be.Infrastructure.Email;
using be.Infrastructure.Persistence;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace be.Tests;
// @external @smtp. Invoked explicitly through the acceptance executable, never by dotnet test or CI.
public static class ExternalSmtpAcceptance
{
    public static async Task<int> RunAsync(string repository, bool outboxOnly = false)
    {
        if (Environment.GetEnvironmentVariable("AMAARA_RUN_SMTP_ACCEPTANCE") != "1")
            throw new InvalidOperationException("SMTP acceptance requires explicit opt-in.");
        var connection = Environment.GetEnvironmentVariable("AMAARA_TEST_SQL_CONNECTION") ?? throw new InvalidOperationException("An isolated SQL test database is required.");
        var sql = new SqlConnectionStringBuilder(connection);
        if (!sql.InitialCatalog.StartsWith("AmaaraPhase6Tests_Smtp_", StringComparison.Ordinal))
            throw new InvalidOperationException("SMTP acceptance requires a separate disposable SMTP database.");
        var configuration = new ConfigurationBuilder().AddJsonFile(Path.Combine(repository, "be", "appsettings.Development.Local.json")).Build();
        foreach (var key in new[]
        {
            "Email:Username",
            "Email:Password",
            "Email:FromEmail"
        }

        )
            if (string.IsNullOrWhiteSpace(configuration[key]))
                throw new InvalidOperationException(key + " is required for real SMTP acceptance.");
        var recipient = Environment.GetEnvironmentVariable("AMAARA_SMTP_TEST_RECIPIENT") ?? configuration["Email:Username"];
        if (string.IsNullOrWhiteSpace(recipient))
            throw new InvalidOperationException("Configure a private test recipient locally.");
        var smtp = new SmtpEmailService(configuration, NullLogger<SmtpEmailService>.Instance);
        var evidence = new Dictionary<string, object?>
        {
            ["StartedUtc"] = DateTime.UtcNow,
            ["External"] = "SMTP",
            ["FixtureDatabase"] = sql.InitialCatalog,
            ["Recipient"] = "Configured mailbox or private override; address omitted",
            ["RealDatatransPayment"] = false,
            ["InboxReceipt"] = "Awaiting recipient confirmation"
        };
        try
        {
            if (outboxOnly)
            {
                using var previous = JsonDocument.Parse(await File.ReadAllTextAsync(Path.Combine(repository, ".runtime", "phase6b-smtp-evidence.json")));
                if (!previous.RootElement.GetProperty("AccountConfirmationSmtpAccepted").GetBoolean() || !previous.RootElement.GetProperty("IdentityConfirmationVerified").GetBoolean())
                    throw new InvalidOperationException("Outbox-only continuation requires recorded successful confirmation evidence.");
                evidence["AccountConfirmationSmtpAccepted"] = true;
                evidence["IdentityConfirmationVerified"] = true;
                evidence["AccountConfirmationStartedUtc"] = previous.RootElement.GetProperty("StartedUtc").GetDateTime();
            }
            else
            {
                await using var factory = new AcceptanceApiFactory();
                factory.Email.Delivery = smtp;
                using var api = factory.CreateClient();
                var password = "Qa4!" + Convert.ToHexString(RandomNumberGenerator.GetBytes(20));
                var registration = await api.PostAsJsonAsync("/api/v1/auth/register", new { name = "Disposable Phase 6B SMTP acceptance", email = recipient, password, confirmPassword = password });
                registration.EnsureSuccessStatusCode();
                evidence["AccountConfirmationSmtpAccepted"] = true;
                var url = new Uri(factory.Email.Confirmation(recipient)!);
                var query = url.Fragment[(url.Fragment.IndexOf('?') + 1)..].Split('&').Select(p => p.Split('=', 2)).ToDictionary(p => Uri.UnescapeDataString(p[0]), p => Uri.UnescapeDataString(p[1]));
                var confirmation = await api.PostAsJsonAsync("/api/v1/auth/confirm-email", new { userId = query["userId"], token = query["token"] });
                confirmation.EnsureSuccessStatusCode();
                evidence["IdentityConfirmationVerified"] = true;
            }

            var options = new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options;
            await using var fixture = new CommerceFixture(new ApplicationDbContext(options));
            await fixture.Db.Database.MigrateAsync();
            if (await fixture.Db.CommerceNotifications.AnyAsync())
                throw new InvalidOperationException("Use a fresh SMTP acceptance database to avoid sending retained test records.");
            var prefix = "smtp-" + Guid.NewGuid().ToString("N");
            var user = new User
            {
                Id = prefix,
                UserName = prefix + "@example.test",
                Email = recipient,
                Name = "Disposable SMTP fixture",
                EmailConfirmed = true
            };
            var product = new Product
            {
                Name = "Disposable SMTP fixture",
                Slug = prefix,
                Price = 100,
                BasePrice = 100,
                Stock = 10
            };
            var address = new Address
            {
                User = user,
                RecipientName = user.Name,
                AddressLine1 = "Disposable test address",
                City = "Colombo",
                Country = "Sri Lanka",
                Phone = "+94770000000"
            };
            var method = new ShippingMethod
            {
                Code = prefix.ToUpperInvariant(),
                Name = "Disposable test delivery",
                BasePrice = 25,
                IsDevelopmentValue = true,
                ShippingZone = new ShippingZone
                {
                    Name = prefix,
                    Country = "Sri Lanka"
                }
            };
            fixture.Settings.AllowDevelopmentShipping = true;
            fixture.Db.AddRange(user, product, address, method);
            await fixture.Db.SaveChangesAsync();
            fixture.Db.CartItems.Add(new CartItem { UserId = user.Id, ProductId = product.Id, Quantity = 2 });
            await fixture.Db.SaveChangesAsync();
            fixture.Db.ChangeTracker.Clear(); // Requote persisted SQL timestamps, matching separate HTTP request scopes.
            var request = new be.Application.Features.Checkout.CheckoutSessionRequest
            {
                AddressId = address.Id,
                ShippingMethodId = method.Id,
                IdempotencyKey = Guid.NewGuid()
            };
            request.QuoteVersion = (await fixture.Pricing.CalculateAsync(user.Id, request, default)).Quote.QuoteVersion;
            var session = await fixture.Checkout.CreateAsync(user.Id, request, default);
            var payment = await fixture.Db.Payments.SingleAsync(p => p.Id == session.PaymentId);
            await fixture.Payments.ApplyAsync(fixture.State(payment), "smtp-test-fixture-only", default);
            var notification = await fixture.Db.CommerceNotifications.SingleAsync(n => n.OrderId == session.OrderId);
            notification.Subject = "[AMAARA PHASE 6B TEST] " + notification.Subject;
            notification.Body = "DISPOSABLE ACCEPTANCE TEST. No real Datatrans payment was taken.\n\n" + notification.Body;
            await fixture.Db.SaveChangesAsync();
            evidence["OrderId"] = session.OrderId;
            evidence["PaymentId"] = session.PaymentId;
            evidence["NotificationId"] = notification.Id;
            evidence["OrderNumber"] = (await fixture.Db.Orders.SingleAsync(o => o.Id == session.OrderId)).OrderNumber;
            var transport = new RestoredTransport(smtp);
            using var services = new ServiceCollection().AddSingleton(fixture.Db).AddSingleton<IEmailService>(transport).BuildServiceProvider();
            using var worker = new CommerceMaintenanceService(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<CommerceMaintenanceService>.Instance);
            await worker.ProcessNotificationsAsync(services, default);
            fixture.Db.ChangeTracker.Clear();
            notification = await fixture.Db.CommerceNotifications.SingleAsync(n => n.Id == notification.Id);
            if (notification.SentAt != null || notification.Attempts != 1 || (await fixture.Db.Payments.SingleAsync(p => p.Id == session.PaymentId)).Status != PaymentStatus.Paid)
                throw new InvalidOperationException("Outbox outage invariant failed.");
            evidence["OutageRetainedPaidOrderAndUnsentOutbox"] = true;
            transport.Available = true;
            notification.LastAttemptAt = DateTime.UtcNow.AddMinutes(-6);
            await fixture.Db.SaveChangesAsync();
            await worker.ProcessNotificationsAsync(services, default);
            fixture.Db.ChangeTracker.Clear();
            notification = await fixture.Db.CommerceNotifications.SingleAsync(n => n.Id == notification.Id);
            evidence["OrderOutboxSmtpAccepted"] = notification.SentAt.HasValue;
            evidence["Attempts"] = notification.Attempts;
            evidence["SentAt"] = notification.SentAt;
            if (notification.SentAt == null)
                throw new InvalidOperationException("SMTP did not accept the order message.");
            await worker.ProcessNotificationsAsync(services, default);
            evidence["DuplicateProcessingDoesNotResend"] = transport.Delivered == 1;
            evidence["Status"] = "SMTP server accepted confirmation and outbox message; inbox receipt unverified";
            Console.WriteLine("SMTP accepted account confirmation and Phase 6B test order message; outage/retry assertions passed.");
            return 0;
        }
        catch (Exception exception)
        {
            evidence["Status"] = "FAILED";
            evidence["FailureType"] = exception.GetType().Name;
            if (exception is be.Domain.Common.CommerceException commerce)
                evidence["FailureCode"] = commerce.Code;
            Console.WriteLine("External SMTP acceptance failed; safe evidence records the exception type only.");
            return 2;
        }
        finally
        {
            var path = Path.Combine(repository, ".runtime", "phase6b-smtp-evidence.json");
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(evidence, new JsonSerializerOptions { WriteIndented = true }));
        }
    }

    private sealed class RestoredTransport(IEmailService smtp) : IEmailService
    {
        public bool Available { get; set; }
        public int Delivered { get; private set; }

        public async Task SendTransactionAsync(User user, string subject, string body, CancellationToken ct = default)
        {
            if (!Available)
                throw new HttpRequestException("Simulated temporary SMTP outage");
            await smtp.SendTransactionAsync(user, subject, body, ct);
            Delivered++;
        }

        public Task SendEmailConfirmationAsync(User user, string url, CancellationToken ct = default) => throw new NotSupportedException();
        public Task SendPasswordResetAsync(User user, string url, CancellationToken ct = default) => throw new NotSupportedException();
    }
}
