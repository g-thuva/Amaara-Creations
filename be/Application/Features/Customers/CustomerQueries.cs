using be.Domain.Identity;
using be.Domain.Orders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Domain.Common;

namespace be.Application.Features.Customers;
public sealed class CustomerQueries(IApplicationDbContext _context, UserManager<User> _userManager, RoleManager<IdentityRole> _roleManager)
{
    public async Task<object?> GetAllCustomers(string? search = null, int pageNumber = 1, int pageSize = 20)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var customersRole = await _roleManager.FindByNameAsync("Customer");
        if (customersRole == null)
        {
            return new
            {
                Customers = new List<CustomerResponse>(),
                TotalCount = 0,
                PageNumber = 1,
                PageSize = pageSize,
                TotalPages = 0
            };
        }

        var roleId = customersRole.Id;
        var query = _context.Users.AsNoTracking().Where(u => _context.UserRoles.Any(ur => ur.RoleId == roleId && ur.UserId == u.Id));
        // Search by name, email, or phone
        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(u => u.Name.Contains(search) || u.Email!.Contains(search) || (u.Phone != null && u.Phone.Contains(search)));
        }

        // Get total count before pagination
        var totalCount = await query.CountAsync();
        // Apply pagination
        var customers = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new CustomerResponse { Id = u.Id, Name = u.Name, Email = u.Email ?? string.Empty, Phone = u.Phone, Address = u.Address, AvatarUrl = u.AvatarUrl, CreatedAt = u.CreatedAt, UpdatedAt = u.UpdatedAt })
            .ToListAsync();
        return new
        {
            Customers = customers,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<CustomerDetailResponse> GetCustomerDetails(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            throw new CommerceException("REPORTING_NOT_FOUND", "Customer not found", 404);
        }

        // Check if user is a customer
        var roles = await _userManager.GetRolesAsync(user);
        if (!roles.Contains("Customer"))
        {
            throw new CommerceException("REPORTING_REQUEST_INVALID", "User is not a customer", 400);
        }

        // Get customer statistics
        var stats = await GetCustomerStatistics(id);
        var response = new CustomerDetailResponse
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email ?? string.Empty,
            Phone = user.Phone,
            Address = user.Address,
            AvatarUrl = user.AvatarUrl,
            CreatedAt = user.CreatedAt,
            UpdatedAt = user.UpdatedAt,
            Stats = stats
        };
        return response;
    }

    public async Task<object?> GetCustomerOrders(string id, int pageNumber = 1, int pageSize = 20)
    {
        pageNumber = Math.Clamp(pageNumber, 1, 1_000_000);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            throw new CommerceException("REPORTING_NOT_FOUND", "Customer not found", 404);
        }

        var query = _context.Orders.AsNoTracking().Where(o => o.UserId == id);
        var totalCount = await query.CountAsync();
        var orders = await query.OrderByDescending(o => o.OrderDate)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new { Id = o.Id, OrderNumber = o.OrderNumber, OrderDate = o.OrderDate, Total = o.Total, Currency = o.Currency, Status = o.Status.ToString(), ShippingAddress = o.ShippingAddress, ShippingCity = o.ShippingCity, TotalItems = o.OrderItems.Sum(oi => oi.Quantity) })
            .ToListAsync();
        return new
        {
            Orders = orders,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }

    public async Task<CustomerStatsResponse> GetCustomerStats(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            throw new CommerceException("REPORTING_NOT_FOUND", "Customer not found", 404);
        }

        var stats = await GetCustomerStatistics(id);
        return stats;
    }

    // Helper method to get customer statistics
    private async Task<CustomerStatsResponse> GetCustomerStatistics(string userId)
    {
        var orders = await _context.Orders.AsNoTracking().Where(o => o.UserId == userId)
            .GroupBy(o => o.UserId)
            .Select(group => new
            {
                Total = group.Count(),
                Pending = group.Count(o => o.Status == OrderStatus.Pending),
                Processing = group.Count(o => o.Status == OrderStatus.Processing),
                Shipped = group.Count(o => o.Status == OrderStatus.Shipped),
                Delivered = group.Count(o => o.Status == OrderStatus.Delivered),
                Cancelled = group.Count(o => o.Status == OrderStatus.Cancelled),
                Spent = group.Sum(o => o.Currency == Money.DefaultCurrency && o.Status != OrderStatus.Cancelled ? o.Total : 0m),
                Last = group.Max(o => (DateTime?)o.OrderDate)
            }).SingleOrDefaultAsync();
        var totalCartItems = await _context.CartItems.CountAsync(c => c.UserId == userId);
        var totalWishlistItems = await _context.WishlistItems.CountAsync(w => w.UserId == userId);
        var totalReviews = await _context.Reviews.CountAsync(r => r.UserId == userId);
        return new CustomerStatsResponse
        {
            TotalOrders = orders?.Total ?? 0,
            PendingOrders = orders?.Pending ?? 0,
            ProcessingOrders = orders?.Processing ?? 0,
            ShippedOrders = orders?.Shipped ?? 0,
            DeliveredOrders = orders?.Delivered ?? 0,
            CancelledOrders = orders?.Cancelled ?? 0,
            TotalSpent = orders?.Spent ?? 0m,
            TotalCartItems = totalCartItems,
            TotalWishlistItems = totalWishlistItems,
            TotalReviews = totalReviews,
            LastOrderDate = orders?.Last
        };
    }
}
