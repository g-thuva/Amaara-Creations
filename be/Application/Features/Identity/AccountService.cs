using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Domain.Common;

namespace be.Application.Features.Identity;
public class AccountService
{
    private readonly UserManager<User> _userManager;
    private readonly IApplicationDbContext _context;
    public AccountService(UserManager<User> userManager, IApplicationDbContext context, IRequestActor actor)
    {
        _userManager = userManager;
        _context = context;
        _actor = actor;
    }

    public async Task<UserProfileResponse> GetProfile()
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("IDENTITY_UNAUTHORIZED", "User not found", 401);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new CommerceException("IDENTITY_NOT_FOUND", "User not found", 404);
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
        return response;
    }

    public async Task<UserProfileResponse> UpdateProfile(UpdateProfileRequest request)
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("IDENTITY_UNAUTHORIZED", "User not found", 401);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new CommerceException("IDENTITY_NOT_FOUND", "User not found", 404);
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
            throw new CommerceException("IDENTITY_REQUEST_INVALID", "Failed to update profile", 400);
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
        return response;
    }

    public async Task<UserStatsResponse> GetUserStats()
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("IDENTITY_UNAUTHORIZED", "User not found", 401);
        }

        var totalOrders = await _context.Orders.Where(o => o.UserId == userId).CountAsync();
        var totalWishlistItems = await _context.WishlistItems.Where(w => w.UserId == userId).CountAsync();
        var totalCartItems = await _context.CartItems.Where(c => c.UserId == userId).CountAsync();
        var totalReviews = await _context.Reviews.Where(r => r.UserId == userId).CountAsync();
        var totalSpent = await _context.Orders.Where(o => o.UserId == userId && o.Status != be.Domain.Orders.OrderStatus.Cancelled).SumAsync(o => (decimal? )o.Total) ?? 0;
        var response = new UserStatsResponse
        {
            TotalOrders = totalOrders,
            TotalWishlistItems = totalWishlistItems,
            TotalCartItems = totalCartItems,
            TotalReviews = totalReviews,
            TotalSpent = totalSpent
        };
        return response;
    }

    public async Task<object?> GetAvatar()
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("IDENTITY_UNAUTHORIZED", "User not found", 401);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new CommerceException("IDENTITY_NOT_FOUND", "User not found", 404);
        }

        return new
        {
            avatarUrl = user.AvatarUrl ?? string.Empty
        };
    }

    public async Task<object?> UpdateAvatar(UpdateAvatarRequest request)
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("IDENTITY_UNAUTHORIZED", "User not found", 401);
        }

        var user = await _userManager.FindByIdAsync(userId);
        if (user == null)
        {
            throw new CommerceException("IDENTITY_NOT_FOUND", "User not found", 404);
        }

        user.AvatarUrl = request.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new CommerceException("IDENTITY_REQUEST_INVALID", "Failed to update avatar", 400);
        }

        return new
        {
            message = "Avatar updated successfully",
            avatarUrl = user.AvatarUrl
        };
    }

    private readonly IRequestActor _actor;
}
