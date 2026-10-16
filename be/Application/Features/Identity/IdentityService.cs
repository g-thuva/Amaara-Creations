using System.Security.Claims;
using be.Application.Abstractions.Email;
using be.Application.Abstractions.Identity;
using be.Domain.Common;
using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace be.Application.Features.Identity;
public sealed class IdentityService(
        UserManager<User> users,
        SignInManager<User> signIn,
        ITokenService tokens,
        IRefreshSessionService sessions,
        IEmailService email,
        IConfiguration configuration,
        IRequestActor actor)
{
    public async Task RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (await users.FindByEmailAsync(request.Email) != null)
            throw new CommerceException("EMAIL_ALREADY_REGISTERED", "Email is already registered. Try signing in or resetting your password.");
        var user = new User
        {
            UserName = request.Email,
            Email = request.Email,
            Name = request.Name.Trim(),
            EmailConfirmed = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        EnsureSuccess(await users.CreateAsync(user, request.Password), "REGISTRATION_FAILED", "Registration failed.");
        await users.AddToRoleAsync(user, AppRoles.Customer);
        await SendConfirmationAsync(user, ct);
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await users.FindByIdAsync(request.UserId);
        if (user == null || !(await users.ConfirmEmailAsync(user, Uri.UnescapeDataString(request.Token))).Succeeded)
            throw new CommerceException("EMAIL_CONFIRMATION_INVALID", "Invalid or expired verification link.");
    }

    public async Task ResendConfirmationAsync(ResendConfirmationRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is { EmailConfirmed: false })
            await SendConfirmationAsync(user, ct);
    }

    public async Task<AuthenticatedSession> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user == null)
            throw InvalidLogin();
        var result = await signIn.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
        if (result.IsLockedOut || !result.Succeeded)
            throw InvalidLogin();
        if (!user.EmailConfirmed)
            throw new CommerceException("EMAIL_CONFIRMATION_REQUIRED", "Email verification is required before signing in.", 401, new Dictionary<string, object?> { ["requiresEmailConfirmation"] = true });
        var roles = await users.GetRolesAsync(user);
        var session = await sessions.CreateSessionAsync(user, actor.Session, ct);
        return new(CreateResponse(user, roles), session.RawToken, session.Session.ExpiresAt);
    }

    public async Task<AuthenticatedSession?> RefreshAsync(string? rawToken, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
            return null;
        var rotation = await sessions.RotateAsync(rawToken, actor.Session, ct);
        return rotation == null ? null : new(CreateResponse(rotation.User, rotation.Roles), rotation.RawToken, rotation.Session.ExpiresAt);
    }

    public async Task LogoutAsync(string? rawToken, CancellationToken ct)
    {
        await sessions.RevokeAsync(rawToken, ct);
        await signIn.SignOutAsync();
    }

    public async Task LogoutAllAsync(CancellationToken ct)
    {
        var id = actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (id != null)
            await sessions.RevokeAllForUserAsync(id, ct);
    }

    public async Task<UserDto> CurrentUserAsync()
    {
        var user = await CurrentUser();
        return ToUserDto(user, await users.GetRolesAsync(user));
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email);
        if (user is not { EmailConfirmed: true })
            return;
        var token = await users.GeneratePasswordResetTokenAsync(user);
        await email.SendPasswordResetAsync(user, FrontendUrl("/reset-password", new Dictionary<string, string?> { ["email"] = user.Email, ["token"] = Uri.EscapeDataString(token) }), ct);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct)
    {
        var user = await users.FindByEmailAsync(request.Email) ?? throw new CommerceException("PASSWORD_RESET_INVALID", "Invalid or expired password reset link.");
        EnsureSuccess(await users.ResetPasswordAsync(user, Uri.UnescapeDataString(request.Token), request.NewPassword), "PASSWORD_RESET_INVALID", "Invalid or expired password reset link.");
        await sessions.RevokeAllForUserAsync(user.Id, ct);
    }

    public async Task ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct)
    {
        var user = await CurrentUser();
        EnsureSuccess(await users.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword), "PASSWORD_CHANGE_FAILED", "Password change failed.");
        await sessions.RevokeAllForUserAsync(user.Id, ct);
    }

    private async Task<User> CurrentUser()
    {
        var id = actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        return (id == null ? null : await users.FindByIdAsync(id)) ?? throw new CommerceException("AUTHENTICATION_REQUIRED", "User is not authenticated.", 401);
    }

    private static CommerceException InvalidLogin() => new("LOGIN_FAILED", "Invalid email or password.", 401);
    private static void EnsureSuccess(IdentityResult result, string code, string message)
    {
        if (!result.Succeeded)
            throw new CommerceException(code, message, details: new Dictionary<string, object?> { ["errors"] = result.Errors.Select(e => e.Description).ToArray() });
    }

    private AuthResponse CreateResponse(User user, IList<string> roles) => new()
    {
        Token = tokens.GenerateToken(user, roles),
        User = ToUserDto(user, roles),
        ExpiresAt = tokens.GetAccessTokenExpiry()
    };
    private static UserDto ToUserDto(User user, IList<string> roles) => new()
    {
        Id = user.Id,
        Name = user.Name,
        Email = user.Email ?? string.Empty,
        Phone = user.Phone,
        Address = user.Address,
        AvatarUrl = user.AvatarUrl,
        EmailConfirmed = user.EmailConfirmed,
        Role = roles.FirstOrDefault() ?? AppRoles.Customer,
        Roles = roles.ToList()
    };
    private async Task SendConfirmationAsync(User user, CancellationToken ct)
    {
        var token = await users.GenerateEmailConfirmationTokenAsync(user);
        await email.SendEmailConfirmationAsync(user, FrontendUrl("/verify-email", new Dictionary<string, string?> { ["userId"] = user.Id, ["token"] = Uri.EscapeDataString(token), ["email"] = user.Email }), ct);
    }

    private string FrontendUrl(string path, IDictionary<string, string?> query)
    {
        var origin = (configuration["Authentication:FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
        var queryString = string.Join("&", query.Where(p => !string.IsNullOrWhiteSpace(p.Value)).Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}"));
        return $"{origin}/#{path}?{queryString}";
    }
}
