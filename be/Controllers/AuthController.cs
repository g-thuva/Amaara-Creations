using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using be.DTOs.Auth;
using be.Models;
using be.Services;
using System.Security.Claims;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ITokenService _tokenService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            UserManager<User> userManager,
            SignInManager<User> signInManager,
            RoleManager<IdentityRole> roleManager,
            ITokenService tokenService,
            ILogger<AuthController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _tokenService = tokenService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Log the incoming request for debugging
                _logger.LogInformation("Register request received: Email={Email}, Name={Name}", 
                    request?.Email ?? "null", request?.Name ?? "null");

                if (request == null)
                {
                    return BadRequest(new { message = "Request body is required" });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                        .Where(x => x.Value?.Errors.Count > 0)
                        .SelectMany(x => x.Value.Errors.Select(e => new { Field = x.Key, Message = e.ErrorMessage }))
                        .ToList();
                    
                    _logger.LogWarning("Validation failed: {Errors}", string.Join(", ", errors.Select(e => $"{e.Field}: {e.Message}")));
                    
                    return BadRequest(new { 
                        message = "Validation failed", 
                        errors = errors 
                    });
                }

                // Check if user already exists
                var existingUser = await _userManager.FindByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    return BadRequest(new { message = "Email is already registered" });
                }

                // Create new user
                var user = new User
                {
                    UserName = request.Email,
                    Email = request.Email,
                    Name = request.Name,
                    EmailConfirmed = true // Set to true for development, use email confirmation in production
                };

                var result = await _userManager.CreateAsync(user, request.Password);

                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Registration failed", errors = result.Errors });
                }

                // Assign default role (Customer)
                await _userManager.AddToRoleAsync(user, "Customer");

                // Generate token
                var roles = await _userManager.GetRolesAsync(user);
                var token = _tokenService.GenerateToken(user, roles);
                var refreshToken = _tokenService.GenerateRefreshToken();

                var response = new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email ?? string.Empty,
                        Phone = user.Phone,
                        Address = user.Address,
                        AvatarUrl = user.AvatarUrl,
                        Role = roles.FirstOrDefault() ?? "Customer"
                    },
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new { message = "An error occurred during registration" });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);

                if (!result.Succeeded)
                {
                    return Unauthorized(new { message = "Invalid email or password" });
                }

                // Generate token
                var roles = await _userManager.GetRolesAsync(user);
                var token = _tokenService.GenerateToken(user, roles);
                var refreshToken = _tokenService.GenerateRefreshToken();

                var response = new AuthResponse
                {
                    Token = token,
                    RefreshToken = refreshToken,
                    User = new UserDto
                    {
                        Id = user.Id,
                        Name = user.Name,
                        Email = user.Email ?? string.Empty,
                        Phone = user.Phone,
                        Address = user.Address,
                        AvatarUrl = user.AvatarUrl,
                        Role = roles.FirstOrDefault() ?? "Customer"
                    },
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new { message = "An error occurred during login" });
            }
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            try
            {
                await _signInManager.SignOutAsync();
                return Ok(new { message = "Logged out successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new { message = "An error occurred during logout" });
            }
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Check if user is authenticated
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    return Unauthorized(new { message = "User is not authenticated" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                var userDto = new UserDto
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email ?? string.Empty,
                    Phone = user.Phone,
                    Address = user.Address,
                    AvatarUrl = user.AvatarUrl,
                    Role = roles.FirstOrDefault() ?? "Customer"
                };

                return Ok(userDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpGet("check-header")]
        public IActionResult CheckHeader()
        {
            var authHeader = Request.Headers["Authorization"].ToString();
            var hasAuth = !string.IsNullOrEmpty(authHeader);
            
            return Ok(new 
            { 
                message = "Header check endpoint (no auth required)",
                hasAuthorizationHeader = hasAuth,
                authorizationHeaderPrefix = hasAuth && authHeader.Length > 20 
                    ? authHeader.Substring(0, 20) + "..." 
                    : authHeader,
                allHeaders = Request.Headers.Select(h => new { h.Key, Value = h.Value.ToString() }).ToList()
            });
        }

        [HttpGet("test-auth")]
        [Authorize]
        public IActionResult TestAuth()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var email = User.FindFirstValue(ClaimTypes.Email);
                var name = User.FindFirstValue(ClaimTypes.Name);
                var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
                
                return Ok(new 
                { 
                    message = "Authentication successful!",
                    userId = userId,
                    email = email,
                    name = name,
                    roles = roles,
                    isAuthenticated = User.Identity?.IsAuthenticated ?? false
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in test auth");
                return StatusCode(500, new { message = "An error occurred" });
            }
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            try
            {
                // Check if user is authenticated
                if (!User.Identity?.IsAuthenticated ?? true)
                {
                    _logger.LogWarning("Change password failed: User is not authenticated");
                    return Unauthorized(new { message = "User is not authenticated" });
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    _logger.LogWarning("User ID not found in token claims");
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Password change failed", errors = result.Errors });
                }

                return Ok(new { message = "Password changed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { message = "An error occurred while changing password" });
            }
        }

        [HttpPost("refresh-token")]
        public Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Task.FromResult<IActionResult>(BadRequest(ModelState));
                }

                // Extract user ID from the current token (if expired, extract from token directly)
                // For simplicity, we'll require the user to send their email with refresh token
                // In production, store refresh tokens in database with user mapping
                
                // Note: This is a simplified implementation. 
                // In production, you should store refresh tokens in the database 
                // and validate them properly.
                
                // For now, we'll just generate a new token if refresh token is provided
                // You may need to send user email/ID with refresh token or store refresh tokens in DB
                
                return Task.FromResult<IActionResult>(BadRequest(new { message = "Refresh token implementation requires user identifier. Please use login endpoint or implement refresh token storage." }));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return Task.FromResult<IActionResult>(StatusCode(500, new { message = "An error occurred while refreshing token" }));
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    // Don't reveal if user exists for security
                    return Ok(new { message = "If an account with that email exists, a password reset link has been sent." });
                }

                // Generate password reset token
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                // In production, send email with token
                // For now, we'll return the token (NEVER do this in production!)
                // TODO: Send email with reset link containing token
                
                _logger.LogWarning("Password reset token generated for {Email}. Token: {Token}", request.Email, token);

                // In production, remove the token from response and send via email
                return Ok(new 
                { 
                    message = "Password reset token generated. In production, this would be sent via email.",
                    // Remove this in production!
                    token = token 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating forgot password token");
                return StatusCode(500, new { message = "An error occurred while processing forgot password request" });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    return BadRequest(new { message = "Invalid email or token" });
                }

                var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);

                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Password reset failed", errors = result.Errors });
                }

                return Ok(new { message = "Password reset successfully. Please login with your new password." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting password");
                return StatusCode(500, new { message = "An error occurred while resetting password" });
            }
        }
    }
}

