using be.Domain.Identity;
using be.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using Xunit;

namespace be.Tests.Unit.Identity;

public sealed class EmailOtpTokenTests
{
    private static (UserManager<User> Users, ServiceProvider Provider) CreateUserManager()
    {
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddDebug());
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString("N")));

        services.AddIdentity<User, IdentityRole>(options =>
        {
            options.Password.RequireDigit = false;
            options.Password.RequireLowercase = false;
            options.Password.RequireUppercase = false;
            options.Password.RequireNonAlphanumeric = false;
            options.Password.RequiredLength = 1;
            options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
            options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        var provider = services.BuildServiceProvider();
        var manager = provider.GetRequiredService<UserManager<User>>();
        return (manager, provider);
    }

    [Fact]
    public async Task EmailConfirmationToken_Is6DigitNumericCode_AndConfirmsSuccessfully()
    {
        var (users, provider) = CreateUserManager();
        await using var _ = provider;

        var user = new User
        {
            UserName = "testotp@example.com",
            Email = "testotp@example.com",
            Name = "OTP Test User",
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        var createResult = await users.CreateAsync(user, "Password123!");
        Assert.True(createResult.Succeeded);

        // Generate email confirmation token
        var code = await users.GenerateEmailConfirmationTokenAsync(user);

        // Must be exactly 6 digits
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(Regex.IsMatch(code, @"^\d{6}$"), $"Expected 6 numeric digits but got '{code}'");

        // Verify with valid code
        var confirmResult = await users.ConfirmEmailAsync(user, code);
        Assert.True(confirmResult.Succeeded);
        Assert.True(user.EmailConfirmed);

        // Verify with wrong code fails
        var failResult = await users.ConfirmEmailAsync(user, "000000");
        Assert.False(failResult.Succeeded);
    }

    [Fact]
    public async Task PasswordResetToken_Is6DigitNumericCode_AndResetsSuccessfully()
    {
        var (users, provider) = CreateUserManager();
        await using var _ = provider;

        var user = new User
        {
            UserName = "resetotp@example.com",
            Email = "resetotp@example.com",
            Name = "Reset Test User",
            EmailConfirmed = true,
            SecurityStamp = Guid.NewGuid().ToString("N")
        };

        var createResult = await users.CreateAsync(user, "OldPassword123!");
        Assert.True(createResult.Succeeded);

        // Generate password reset token
        var code = await users.GeneratePasswordResetTokenAsync(user);

        // Must be exactly 6 digits
        Assert.NotNull(code);
        Assert.Equal(6, code.Length);
        Assert.True(Regex.IsMatch(code, @"^\d{6}$"), $"Expected 6 numeric digits but got '{code}'");

        // Attempt reset with invalid code should fail
        var invalidResult = await users.ResetPasswordAsync(user, "999999", "NewPassword123!");
        Assert.False(invalidResult.Succeeded);

        // Attempt reset with valid code should succeed
        var resetResult = await users.ResetPasswordAsync(user, code, "NewPassword123!");
        Assert.True(resetResult.Succeeded);

        // Check that new password works
        Assert.True(await users.CheckPasswordAsync(user, "NewPassword123!"));
    }
}
