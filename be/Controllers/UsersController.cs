using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using be.Data;
using be.DTOs.User;
using be.Models;
using System.Security.Claims;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All user endpoints require authentication
    public class UsersController : ControllerBase
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UsersController> _logger;

        public UsersController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            ILogger<UsersController> logger)
        {
            _userManager = userManager;
            _context = context;
            _logger = logger;
        }

        // GET: api/users/profile - Get user profile
        [HttpGet("profile")]
        public async Task<ActionResult<UserProfileResponse>> GetProfile()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var roles = await _userManager.GetRolesAsync(user);

                var response = new UserProfileResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email ?? string.Empty,
                    Phone = user.Phone,
                    Address = user.Address,
                    AvatarUrl = user.AvatarUrl,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Roles = roles.ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { message = "An error occurred while retrieving profile" });
            }
        }

        // PUT: api/users/profile - Update user profile
        [HttpPut("profile")]
        public async Task<ActionResult<UserProfileResponse>> UpdateProfile([FromBody] UpdateProfileRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                // Update user properties
                user.Name = request.Name;
                user.Phone = request.Phone;
                user.Address = request.Address;
                user.AvatarUrl = request.AvatarUrl;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Failed to update profile", errors = result.Errors });
                }

                var roles = await _userManager.GetRolesAsync(user);

                var response = new UserProfileResponse
                {
                    Id = user.Id,
                    Name = user.Name,
                    Email = user.Email ?? string.Empty,
                    Phone = user.Phone,
                    Address = user.Address,
                    AvatarUrl = user.AvatarUrl,
                    CreatedAt = user.CreatedAt,
                    UpdatedAt = user.UpdatedAt,
                    Roles = roles.ToList()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { message = "An error occurred while updating profile" });
            }
        }

        // GET: api/users/profile/stats - Get user statistics
        [HttpGet("profile/stats")]
        public async Task<ActionResult<UserStatsResponse>> GetUserStats()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var totalOrders = await _context.Orders
                    .Where(o => o.UserId == userId)
                    .CountAsync();

                var totalWishlistItems = await _context.WishlistItems
                    .Where(w => w.UserId == userId)
                    .CountAsync();

                var totalCartItems = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .CountAsync();

                var totalReviews = await _context.Reviews
                    .Where(r => r.UserId == userId)
                    .CountAsync();

                var totalSpent = await _context.Orders
                    .Where(o => o.UserId == userId && o.Status != Models.OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                var response = new UserStatsResponse
                {
                    TotalOrders = totalOrders,
                    TotalWishlistItems = totalWishlistItems,
                    TotalCartItems = totalCartItems,
                    TotalReviews = totalReviews,
                    TotalSpent = totalSpent
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving statistics" });
            }
        }

        // GET: api/users/profile/avatar - Get user avatar URL
        [HttpGet("profile/avatar")]
        public async Task<ActionResult> GetAvatar()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                return Ok(new { avatarUrl = user.AvatarUrl ?? string.Empty });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user avatar");
                return StatusCode(500, new { message = "An error occurred while retrieving avatar" });
            }
        }

        // POST: api/users/profile/avatar - Update user avatar (set avatar URL)
        [HttpPost("profile/avatar")]
        public async Task<ActionResult> UpdateAvatar([FromBody] UpdateAvatarRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                user.AvatarUrl = request.AvatarUrl;
                user.UpdatedAt = DateTime.UtcNow;

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    return BadRequest(new { message = "Failed to update avatar", errors = result.Errors });
                }

                return Ok(new { message = "Avatar updated successfully", avatarUrl = user.AvatarUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user avatar");
                return StatusCode(500, new { message = "An error occurred while updating avatar" });
            }
        }
    }
}

