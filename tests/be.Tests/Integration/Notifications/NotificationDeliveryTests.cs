using be.Application.Abstractions.Email;
using be.Application.Features.Notifications;
using be.Domain.Identity;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Infrastructure.BackgroundJobs;
using be.Infrastructure.Email;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace be.Tests;
public sealed class NotificationDeliveryTests
{
    [Fact]
    public async Task OutageRetainsPaidOrderAndOutboxThenRetriesExactlyOnce()
    {
        await using var fixture = new CommerceFixture();
        await fixture.SeedAsync();
        var payment = await fixture.StartAsync();
        await fixture.Payments.ApplyAsync(fixture.State(payment), null, default);
        var email = new RecoverableEmail();
        var services = new ServiceCollection().AddSingleton(fixture.Db).AddSingleton<IEmailService>(email).BuildServiceProvider();
        using var worker = new CommerceMaintenanceService(services.GetRequiredService<IServiceScopeFactory>(), NullLogger<CommerceMaintenanceService>.Instance);
        await worker.ProcessNotificationsAsync(services, default);
        fixture.Db.ChangeTracker.Clear();
        var failed = await fixture.Db.CommerceNotifications.SingleAsync();
        Assert.Null(failed.SentAt);
        Assert.Equal(1, failed.Attempts);
        Assert.NotNull(failed.LastAttemptAt);
        Assert.Equal(PaymentStatus.Paid, (await fixture.Db.Payments.SingleAsync()).Status);
        Assert.Equal(OrderStatus.Confirmed, (await fixture.Db.Orders.SingleAsync()).Status);
        Assert.Equal("Consumed", (await fixture.Db.InventoryReservations.SingleAsync()).Status);
        email.Available = true;
        await worker.ProcessNotificationsAsync(services, default);
        Assert.Equal(1, email.Calls); // respects backoff
        failed.LastAttemptAt = DateTime.UtcNow.AddMinutes(-6);
        await fixture.Db.SaveChangesAsync();
        await worker.ProcessNotificationsAsync(services, default);
        fixture.Db.ChangeTracker.Clear();
        var sent = await fixture.Db.CommerceNotifications.SingleAsync();
        Assert.NotNull(sent.SentAt);
        Assert.Equal(2, sent.Attempts);
        Assert.Equal(1, email.Delivered);
        await worker.ProcessNotificationsAsync(services, default);
        Assert.Equal(2, email.Calls);
    }

    [Fact]
    public async Task MissingSmtpCannotMarkTransactionalMessageSent()
    {
        var email = new SmtpEmailService(new Microsoft.Extensions.Configuration.ConfigurationBuilder().Build(), NullLogger<SmtpEmailService>.Instance);
        await Assert.ThrowsAsync<InvalidOperationException>(() => email.SendTransactionAsync(new User { Email = "disposable@example.test" }, "Test", "Test"));
    }

    private sealed class RecoverableEmail : IEmailService
    {
        public bool Available { get; set; }
        public int Calls { get; private set; }
        public int Delivered { get; private set; }

        public Task SendTransactionAsync(User user, string subject, string body, CancellationToken ct = default)
        {
            Calls++;
            if (!Available)
                throw new HttpRequestException("Simulated SMTP outage");
            Assert.Contains("225.00 LKR", body);
            Delivered++;
            return Task.CompletedTask;
        }

        public Task SendEmailConfirmationAsync(User user, string url, CancellationToken ct = default) => Task.CompletedTask;
        public Task SendPasswordResetAsync(User user, string url, CancellationToken ct = default) => Task.CompletedTask;
    }
}
