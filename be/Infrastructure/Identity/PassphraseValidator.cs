using System.Text;
using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace be.Infrastructure.Identity;

// This deliberately local list is a baseline, not a claim of breach-corpus coverage.
// Operators can extend the whole-password list without sending passwords externally.
public sealed class PassphraseValidator(IConfiguration configuration) : IPasswordValidator<User>
{
    public Task<IdentityResult> ValidateAsync(UserManager<User> manager, User user, string? password)
    {
        var count = password?.EnumerateRunes().Count() ?? 0;
        if (count is < 15 or > 128)
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "PasswordLength", Description = "Use 15 to 128 characters. A longer, unique passphrase works well." }));
        var normalized = password!.Normalize(NormalizationForm.FormC).ToUpperInvariant();
        var blocked = new[] { "passwordpassword", "password123456789", "123456789012345", "amaaracreations", "amaaracreations123" }
            .Concat(configuration.GetSection("Authentication:BlockedPasswords").Get<string[]>() ?? []);
        if (blocked.Any(value => value.Normalize(NormalizationForm.FormC).ToUpperInvariant() == normalized) ||
            normalized == user.Email?.Normalize(NormalizationForm.FormC).ToUpperInvariant())
            return Task.FromResult(IdentityResult.Failed(new IdentityError { Code = "PasswordBlocked", Description = "This password is too commonly used. Choose a different, unique passphrase." }));
        return Task.FromResult(IdentityResult.Success);
    }
}
