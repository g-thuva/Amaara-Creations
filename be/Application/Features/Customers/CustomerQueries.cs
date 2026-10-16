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

        var customerUserIds = await _context.UserRoles.Where(ur => ur.RoleId == customersRole.Id).Select(ur => ur.UserId).ToListAsync();
        var query = _context.Users.Where(u => customerUserIds.Contains(u.Id)).AsQueryable();
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
        var user = await _userManager.FindByIdAsync(id);
        if (user == null)
        {
            throw new CommerceException("REPORTING_NOT_FOUND", "Customer not found", 404);
        }

        var query = _context.Orders.Include(o => o.OrderItems).ThenInclude(oi => oi.Product).Where(o => o.UserId == id).AsQueryable();
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
        var totalOrders = await _context.Orders.Where(o => o.UserId == userId).CountAsync();
        var pendingOrders = await _context.Orders.Where(o => o.UserId == userId && o.Status == OrderStatus.Pending).CountAsync();
        var processingOrders = await _context.Orders.Where(o => o.UserId == userId && o.Status == OrderStatus.Processing).CountAsync();
        var shippedOrders = await _context.Orders.Where(o => o.UserId == userId && o.Status == OrderStatus.Shipped).CountAsync();
        var deliveredOrders = await _context.Orders.Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered).CountAsync();
        var cancelledOrders = await _context.Orders.Where(o => o.UserId == userId && o.Status == OrderStatus.Cancelled).CountAsync();
        var totalSpent = await _context.Orders.Where(o => o.UserId == userId && o.Currency == "LKR" && o.Status != OrderStatus.Cancelled)
            .SumAsync(o => (decimal? )o.Total) ?? 0;
        var totalCartItems = await _context.CartItems.Where(c => c.UserId == userId).CountAsync();
        var totalWishlistItems = await _context.WishlistItems.Where(w => w.UserId == userId).CountAsync();
        var totalReviews = await _context.Reviews.Where(r => r.UserId == userId).CountAsync();
        var lastOrderDate = await _context.Orders.Where(o => o.UserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => (DateTime? )o.OrderDate)
            .FirstOrDefaultAsync();
        return new CustomerStatsResponse
        {
            TotalOrders = totalOrders,
            PendingOrders = pendingOrders,
            ProcessingOrders = processingOrders,
            ShippedOrders = shippedOrders,
            DeliveredOrders = deliveredOrders,
            CancelledOrders = cancelledOrders,
            TotalSpent = totalSpent,
            TotalCartItems = totalCartItems,
            TotalWishlistItems = totalWishlistItems,
            TotalReviews = totalReviews,
            LastOrderDate = lastOrderDate
        };
    }
}
