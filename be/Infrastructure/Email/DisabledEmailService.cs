using be.Application.Abstractions.Email;
using be.Domain.Identity;

namespace be.Infrastructure.Email;

public sealed class DisabledEmailService : IEmailService
{
    private static Task Disabled() => Task.FromException(new InvalidOperationException("Email delivery is disabled by configuration."));
    public Task SendEmailConfirmationAsync(User user, string url, CancellationToken ct = default) => Disabled();
    public Task SendPasswordResetAsync(User user, string url, CancellationToken ct = default) => Disabled();
    public Task SendTransactionAsync(User user, string subject, string body, CancellationToken ct = default) => Disabled();
}
