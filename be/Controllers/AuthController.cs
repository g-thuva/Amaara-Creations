using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using be.DTOs.Auth;
using be.Models;
using be.Security;
using be.Services;

namespace be.Controllers
{
    [ApiController]
    [Route("api/v1/auth")]
    public class AuthController : ControllerBase
    {
        private const string GenericLoginMessage = "Invalid email or password.";
        private const string GenericPasswordResetMessage = "If an account with that email exists, password reset instructions have been sent.";

        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IRefreshSessionService _refreshSessionService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            ITokenService tokenService,
            IRefreshSessionService refreshSessionService,
            IEmailService emailService,
            IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
            _refreshSessionService = refreshSessionService;
            _emailService = emailService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
            {
                return BadRequest(new { message = "Email is already registered. Try signing in or resetting your password." });
            }

            var user = new User
            {
                UserName = request.Email,
                Email = request.Email,
                Name = request.Name.Trim(),
                EmailConfirmed = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Registration failed.", errors = result.Errors.Select(e => e.Description) });
            }

            await _userManager.AddToRoleAsync(user, AppRoles.Customer);
            await SendConfirmationEmailAsync(user, cancellationToken);

            return Ok(new
            {
                message = "Registration successful. Check your email to verify your account before signing in.",
                requiresEmailConfirmation = true
            });
        }

        [HttpPost("confirm-email")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailRequest request)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var user = await _userManager.FindByIdAsync(request.UserId);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid or expired verification link." });
            }

            var result = await _userManager.ConfirmEmailAsync(user, Uri.UnescapeDataString(request.Token));
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Invalid or expired verification link." });
            }

            return Ok(new { message = "Email verified. You can now sign in." });
        }

        [HttpPost("resend-confirmation")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendConfirmationRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null && !user.EmailConfirmed)
            {
                await SendConfirmationEmailAsync(user, cancellationToken);
            }

            return Ok(new { message = "If an account requires verification, a confirmation email has been sent." });
        }

        [HttpPost("login")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return Unauthorized(new { message = GenericLoginMessage });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: true);
            if (result.IsLockedOut || !result.Succeeded)
            {
                return Unauthorized(new { message = GenericLoginMessage });
            }

            if (!user.EmailConfirmed)
            {
                return Unauthorized(new { message = "Email verification is required before signing in.", requiresEmailConfirmation = true });
            }

            var roles = await _userManager.GetRolesAsync(user);
            var refreshSession = await _refreshSessionService.CreateSessionAsync(user, HttpContext, cancellationToken);
            SetRefreshCookie(refreshSession.RawToken, refreshSession.Session.ExpiresAt);

            return Ok(CreateAuthResponse(user, roles));
        }

        [HttpPost("refresh")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
        {
            var rawRefreshToken = Request.Cookies[GetRefreshCookieName()];
            if (string.IsNullOrWhiteSpace(rawRefreshToken))
            {
                ClearRefreshCookie();
                return Unauthorized(new { message = "Session expired." });
            }

            var rotation = await _refreshSessionService.RotateAsync(rawRefreshToken, HttpContext, cancellationToken);
            if (rotation == null)
            {
                ClearRefreshCookie();
                return Unauthorized(new { message = "Session expired." });
            }

            SetRefreshCookie(rotation.RawToken, rotation.Session.ExpiresAt);
            return Ok(CreateAuthResponse(rotation.User, rotation.Roles));
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout(CancellationToken cancellationToken)
        {
            await _refreshSessionService.RevokeAsync(Request.Cookies[GetRefreshCookieName()], cancellationToken);
            ClearRefreshCookie();
            await _signInManager.SignOutAsync();
            return Ok(new { message = "Logged out successfully." });
        }

        [HttpPost("logout-all")]
        [Authorize]
        public async Task<IActionResult> LogoutAll(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                await _refreshSessionService.RevokeAllForUserAsync(userId, cancellationToken);
            }

            ClearRefreshCookie();
            return Ok(new { message = "All sessions were revoked." });
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var roles = await _userManager.GetRolesAsync(user);
            return Ok(ToUserDto(user, roles));
        }

        [HttpPost("forgot-password")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user != null && user.EmailConfirmed)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var resetUrl = BuildFrontendUrl("/reset-password", new Dictionary<string, string?>
                {
                    ["email"] = user.Email,
                    ["token"] = Uri.EscapeDataString(token)
                });

                await _emailService.SendPasswordResetAsync(user, resetUrl, cancellationToken);
            }

            return Ok(new { message = GenericPasswordResetMessage });
        }

        [HttpPost("reset-password")]
        [EnableRateLimiting("auth-sensitive")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null)
            {
                return BadRequest(new { message = "Invalid or expired password reset link." });
            }

            var result = await _userManager.ResetPasswordAsync(user, Uri.UnescapeDataString(request.Token), request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Invalid or expired password reset link.", errors = result.Errors.Select(e => e.Description) });
            }

            await _refreshSessionService.RevokeAllForUserAsync(user.Id, cancellationToken);
            ClearRefreshCookie();

            return Ok(new { message = "Password reset successfully. Please sign in again." });
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return Unauthorized(new { message = "User is not authenticated." });
            }

            var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);
            if (!result.Succeeded)
            {
                return BadRequest(new { message = "Password change failed.", errors = result.Errors.Select(e => e.Description) });
            }

            await _refreshSessionService.RevokeAllForUserAsync(user.Id, cancellationToken);
            ClearRefreshCookie();

            return Ok(new { message = "Password changed successfully. Please sign in again." });
        }

        private AuthResponse CreateAuthResponse(User user, IList<string> roles)
        {
            return new AuthResponse
            {
                Token = _tokenService.GenerateToken(user, roles),
                User = ToUserDto(user, roles),
                ExpiresAt = _tokenService.GetAccessTokenExpiry()
            };
        }

        private static UserDto ToUserDto(User user, IList<string> roles)
        {
            return new UserDto
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
        }

        private async Task SendConfirmationEmailAsync(User user, CancellationToken cancellationToken)
        {
            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var confirmationUrl = BuildFrontendUrl("/verify-email", new Dictionary<string, string?>
            {
                ["userId"] = user.Id,
                ["token"] = Uri.EscapeDataString(token),
                ["email"] = user.Email
            });

            await _emailService.SendEmailConfirmationAsync(user, confirmationUrl, cancellationToken);
        }

        private string BuildFrontendUrl(string path, IDictionary<string, string?> query)
        {
            var baseUrl = (_configuration["Authentication:FrontendBaseUrl"] ?? "http://localhost:5173").TrimEnd('/');
            var queryString = string.Join("&", query
                .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
                .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}"));

            return $"{baseUrl}/#{path}?{queryString}";
        }

        private void SetRefreshCookie(string rawToken, DateTime expiresAt)
        {
            Response.Cookies.Append(GetRefreshCookieName(), rawToken, new CookieOptions
            {
                HttpOnly = true,
                Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                Path = "/api/v1/auth",
                Expires = expiresAt
            });
        }

        private void ClearRefreshCookie()
        {
            Response.Cookies.Delete(GetRefreshCookieName(), new CookieOptions
            {
                HttpOnly = true,
                Secure = !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment(),
                SameSite = SameSiteMode.Lax,
                Path = "/api/v1/auth"
            });
        }

        private string GetRefreshCookieName()
        {
            return _configuration["Authentication:RefreshCookieName"] ?? "amaara_refresh";
        }
    }
}
