using System.Net;
using System.Net.Mail;
using be.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace be.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailService> _logger;

        public SmtpEmailService(IConfiguration configuration, ILogger<SmtpEmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailConfirmationAsync(User user, string confirmationUrl, CancellationToken cancellationToken = default)
        {
            var subject = "Confirm your email address";
            var body = $"Please confirm your email address by clicking the following link:\n\n{confirmationUrl}";
            await SendEmailAsync(user.Email!, subject, body, cancellationToken);
        }

        public async Task SendPasswordResetAsync(User user, string resetUrl, CancellationToken cancellationToken = default)
        {
            var subject = "Reset your password";
            var body = $"Please reset your password by clicking the following link:\n\n{resetUrl}";
            await SendEmailAsync(user.Email!, subject, body, cancellationToken);
        }

        private async Task SendEmailAsync(string toEmail, string subject, string body, CancellationToken cancellationToken)
        {
            var emailConfig = _configuration.GetSection("Email");
            var username = emailConfig["Username"];
            var password = emailConfig["Password"];
            var fromEmail = emailConfig["FromEmail"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogWarning("Email configuration is missing. Cannot send email.");
                return;
            }

            try
            {
                using var client = new SmtpClient("smtp.gmail.com", 587)
                {
                    EnableSsl = true,
                    UseDefaultCredentials = false,
                    Credentials = new NetworkCredential(username, password)
                };

                using var mailMessage = new MailMessage(fromEmail, toEmail, subject, body);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {Email} successfully.", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while sending email to {Email}. Error: {Error}", toEmail, ex.Message);
                throw new InvalidOperationException($"Failed to send email to {toEmail}. Error: {ex.Message}", ex);
            }
        }
    }
}
