using be.Models;

namespace be.Services
{
    public interface IEmailService
    {
        Task SendEmailConfirmationAsync(User user, string confirmationUrl, CancellationToken cancellationToken = default);

        Task SendPasswordResetAsync(User user, string resetUrl, CancellationToken cancellationToken = default);
        Task SendTransactionAsync(User user, string subject, string body, CancellationToken cancellationToken = default);
    }
}
