using be.Models;

namespace be.Services
{
    public class DevelopmentEmailService : IEmailService
    {
        private readonly ILogger<DevelopmentEmailService> _logger;
        private readonly IWebHostEnvironment _environment;

        public DevelopmentEmailService(ILogger<DevelopmentEmailService> logger, IWebHostEnvironment environment)
        {
            _logger = logger;
            _environment = environment;
        }

        public Task SendEmailConfirmationAsync(User user, string confirmationUrl, CancellationToken cancellationToken = default)
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogInformation("Development email confirmation link generated for {Email}: {ConfirmationUrl}", user.Email, confirmationUrl);
            }
            else
            {
                _logger.LogWarning("Email confirmation requested for {Email}, but no production email provider is configured.", user.Email);
            }

            return Task.CompletedTask;
        }

        public Task SendPasswordResetAsync(User user, string resetUrl, CancellationToken cancellationToken = default)
        {
            if (_environment.IsDevelopment())
            {
                _logger.LogInformation("Development password reset link generated for {Email}: {ResetUrl}", user.Email, resetUrl);
            }
            else
            {
                _logger.LogWarning("Password reset requested for {Email}, but no production email provider is configured.", user.Email);
            }

            return Task.CompletedTask;
        }
    }
}
