using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Domain.Common;
using be.Application.Abstractions.Storage;

namespace be.Application.Features.Identity;
public class AccountService
{
    private readonly UserManager<User> _userManager;
    private readonly IApplicationDbContext _context;
    private readonly IMediaCleanupQueue? _cleanup;
    public AccountService(UserManager<User> userManager, IApplicationDbContext context, IRequestActor actor, IMediaCleanupQueue? cleanup = null)
    {
        _userManager = userManager;
        _context = context;
        _actor = actor;
        _cleanup = cleanup;
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
        QueuePreviousAvatar(user.AvatarUrl, request.AvatarUrl);
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
        var orderTotals = await _context.Orders.AsNoTracking()
            .Where(o => o.UserId == userId && o.Status != be.Domain.Orders.OrderStatus.Cancelled)
            .GroupBy(o => o.Currency)
            .Select(group => new { Currency = group.Key, Total = group.Sum(o => o.Total) })
            .ToListAsync();
        var totalsByCurrency = orderTotals.ToDictionary(total => total.Currency, total => total.Total, StringComparer.Ordinal);
        var response = new UserStatsResponse
        {
            TotalOrders = totalOrders,
            TotalWishlistItems = totalWishlistItems,
            TotalCartItems = totalCartItems,
            TotalReviews = totalReviews,
            TotalSpent = totalsByCurrency.GetValueOrDefault("LKR"),
            Currency = "LKR",
            OrderTotalsByCurrency = totalsByCurrency
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

        QueuePreviousAvatar(user.AvatarUrl, request.AvatarUrl);
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

    private void QueuePreviousAvatar(string? previous, string? next)
    {
        if (previous != next && previous?.StartsWith("/api/v1/media/public/", StringComparison.Ordinal) == true) _cleanup?.EnqueueDelete(previous);
    }
    private readonly IRequestActor _actor;
}
