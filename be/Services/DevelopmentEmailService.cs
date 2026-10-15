using System.Text.Json;
using be.Models;

namespace be.Services;

// Explicitly selected in Development only. Captures a private local inbox;
// never logs account tokens or claims delivery to a real recipient.
public sealed class DevelopmentEmailService(ILogger<DevelopmentEmailService> logger, IWebHostEnvironment environment) : IEmailService
{
    public Task SendEmailConfirmationAsync(User user, string url, CancellationToken ct = default) => Capture(user, "Verify email (DEVELOPMENT CAPTURE)", url, ct);
    public Task SendPasswordResetAsync(User user, string url, CancellationToken ct = default) => Capture(user, "Reset password (DEVELOPMENT CAPTURE)", url, ct);
    public Task SendTransactionAsync(User user, string subject, string body, CancellationToken ct = default) => Capture(user, "DEVELOPMENT CAPTURE: " + subject, body, ct);
    private async Task Capture(User user, string subject, string body, CancellationToken ct)
    {
        if (!environment.IsDevelopment()) throw new InvalidOperationException("Development email capture is disabled outside Development.");
        var directory = Path.Combine(environment.ContentRootPath, "App_Data", "development-email"); Directory.CreateDirectory(directory);
        await File.WriteAllTextAsync(Path.Combine(directory, Guid.NewGuid().ToString("N") + ".json"), JsonSerializer.Serialize(new { developmentCapture = true, recipient = user.Email, subject, body, createdAt = DateTime.UtcNow }), ct);
        logger.LogInformation("Development email captured in the private local inbox.");
    }
}
