using be.Domain.Common;
using be.Domain.Identity;
using be.Domain.Orders;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;

namespace be.Application.Features.Reporting;
public sealed class DashboardQueries(IApplicationDbContext _context, UserManager<User> _userManager)
{
    public async Task<DashboardStatsResponse> GetDashboardStats()
    {
        var today = DateTime.UtcNow.Date;
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var endOfToday = today.AddDays(1);
        // This legacy dashboard reports CHF only. TEST payments are never business revenue.
        var revenueOrders = _context.Orders.Where(o => o.Currency == Money.DefaultCurrency && o.Status != OrderStatus.Cancelled && !o.Payments.Any(p => (p.ProviderMode == "TEST" || p.ProviderMode == "Sandbox")));
        // Total Revenue (CHF business orders only)
        var totalRevenue = await revenueOrders.SumAsync(o => (decimal? )o.Total) ?? 0;
        // Revenue Today
        var revenueToday = await revenueOrders.Where(o => o.OrderDate >= today && o.OrderDate < endOfToday).SumAsync(o => (decimal? )o.Total) ?? 0;
        // Revenue This Month
        var revenueThisMonth = await revenueOrders.Where(o => o.OrderDate >= thisMonth).SumAsync(o => (decimal? )o.Total) ?? 0;
        // Total Orders
        var totalOrders = await _context.Orders.CountAsync();
        // Orders Today
        var ordersToday = await _context.Orders.Where(o => o.OrderDate >= today && o.OrderDate < endOfToday).CountAsync();
        // Orders This Month
        var ordersThisMonth = await _context.Orders.Where(o => o.OrderDate >= thisMonth).CountAsync();
        // Total Customers (users with role "Customer")
        var totalCustomers = await _userManager.GetUsersInRoleAsync(be.Domain.Identity.AppRoles.Customer);
        var customerCount = totalCustomers.Count;
        // Total Products
        var totalProducts = await _context.Products.CountAsync();
        // Average Rating
        var averageRating = await _context.Reviews.Select(r => (double? )r.Rating).AverageAsync() ?? 0.0;
        // Order Status Breakdown
        var pendingOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Pending).CountAsync();
        var processingOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Processing).CountAsync();
        var shippedOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Shipped).CountAsync();
        var deliveredOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Delivered).CountAsync();
        var response = new DashboardStatsResponse
        {
            TotalRevenue = totalRevenue,
            TotalOrders = totalOrders,
            TotalCustomers = customerCount,
            TotalProducts = totalProducts,
            AverageRating = Math.Round(averageRating, 2),
            RevenueToday = revenueToday,
            RevenueThisMonth = revenueThisMonth,
            OrdersToday = ordersToday,
            OrdersThisMonth = ordersThisMonth,
            PendingOrders = pendingOrders,
            ProcessingOrders = processingOrders,
            ShippedOrders = shippedOrders,
            DeliveredOrders = deliveredOrders
        };
        return response;
    }

    public async Task<RevenueStatsResponse> GetRevenueStats()
    {
        var today = DateTime.UtcNow.Date;
        var endOfToday = today.AddDays(1);
        var thisWeek = today.AddDays(-(int)today.DayOfWeek);
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var thisYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
        var revenueOrders = _context.Orders.Where(o => o.Currency == Money.DefaultCurrency && o.Status != OrderStatus.Cancelled && !o.Payments.Any(p => (p.ProviderMode == "TEST" || p.ProviderMode == "Sandbox")));
        // Total Revenue
        var totalRevenue = await revenueOrders.SumAsync(o => (decimal? )o.Total) ?? 0;
        // Revenue Today
        var revenueToday = await revenueOrders.Where(o => o.OrderDate >= today && o.OrderDate < endOfToday).SumAsync(o => (decimal? )o.Total) ?? 0;
        // Revenue This Week
        var revenueThisWeek = await revenueOrders.Where(o => o.OrderDate >= thisWeek).SumAsync(o => (decimal? )o.Total) ?? 0;
        // Revenue This Month
        var revenueThisMonth = await revenueOrders.Where(o => o.OrderDate >= thisMonth).SumAsync(o => (decimal? )o.Total) ?? 0;
        // Revenue This Year
        var revenueThisYear = await revenueOrders.Where(o => o.OrderDate >= thisYear).SumAsync(o => (decimal? )o.Total) ?? 0;
        // Monthly Revenue for last 12 months
        var reportStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-11);
        var monthlyAmounts = await revenueOrders.Where(o => o.OrderDate >= reportStart)
            .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
            .Select(g => new { g.Key.Year, g.Key.Month, Amount = g.Sum(o => o.Total) })
            .ToListAsync();
        var monthlyRevenues = new List<MonthlyRevenue>();
        for (int i = 11; i >= 0; i--)
        {
            var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
            var monthEnd = monthStart.AddMonths(1);
            var monthRevenue = monthlyAmounts.Where(m => m.Year == monthStart.Year && m.Month == monthStart.Month).Sum(m => m.Amount);
            monthlyRevenues.Add(new MonthlyRevenue { Month = monthStart.ToString("MMMM"), Year = monthStart.Year, Revenue = monthRevenue });
        }

        var response = new RevenueStatsResponse
        {
            TotalRevenue = totalRevenue,
            RevenueToday = revenueToday,
            RevenueThisWeek = revenueThisWeek,
            RevenueThisMonth = revenueThisMonth,
            RevenueThisYear = revenueThisYear,
            MonthlyRevenue = monthlyRevenues
        };
        return response;
    }

    public async Task<OrderStatsResponse> GetOrderStats()
    {
        var today = DateTime.UtcNow.Date;
        var endOfToday = today.AddDays(1);
        var thisWeek = today.AddDays(-(int)today.DayOfWeek);
        var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
        var thisYear = new DateTime(DateTime.UtcNow.Year, 1, 1);
        // Total Orders
        var totalOrders = await _context.Orders.CountAsync();
        // Orders Today
        var ordersToday = await _context.Orders.Where(o => o.OrderDate >= today && o.OrderDate < endOfToday).CountAsync();
        // Orders This Week
        var ordersThisWeek = await _context.Orders.Where(o => o.OrderDate >= thisWeek).CountAsync();
        // Orders This Month
        var ordersThisMonth = await _context.Orders.Where(o => o.OrderDate >= thisMonth).CountAsync();
        // Orders This Year
        var ordersThisYear = await _context.Orders.Where(o => o.OrderDate >= thisYear).CountAsync();
        // Order Status Breakdown
        var pendingOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Pending).CountAsync();
        var processingOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Processing).CountAsync();
        var shippedOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Shipped).CountAsync();
        var deliveredOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Delivered).CountAsync();
        var cancelledOrders = await _context.Orders.Where(o => o.Status == OrderStatus.Cancelled).CountAsync();
        var response = new OrderStatsResponse
        {
            TotalOrders = totalOrders,
            OrdersToday = ordersToday,
            OrdersThisWeek = ordersThisWeek,
            OrdersThisMonth = ordersThisMonth,
            OrdersThisYear = ordersThisYear,
            PendingOrders = pendingOrders,
            ProcessingOrders = processingOrders,
            ShippedOrders = shippedOrders,
            DeliveredOrders = deliveredOrders,
            CancelledOrders = cancelledOrders
        };
        return response;
    }

    public async Task<ProductStatsResponse> GetProductStats()
    {
        // Total Products
        var totalProducts = await _context.Products.CountAsync();
        // Active Products
        var activeProducts = await _context.Products.Where(p => p.IsActive).CountAsync();
        // Inactive Products
        var inactiveProducts = totalProducts - activeProducts;
        // Out of Stock Products
        var outOfStockProducts = await _context.Products.Where(p => p.Stock == 0).CountAsync();
        // Low Stock Products (Stock < 10)
        var lowStockProducts = await _context.Products.Where(p => p.Stock > 0 && p.Stock < 10).CountAsync();
        // Top Selling Products (by quantity sold)
        var topSellingProducts = await _context.OrderItems.Include(oi => oi.Product)
            .Where(oi => oi.Order!.Currency == Money.DefaultCurrency && oi.Order.Status != OrderStatus.Cancelled && !oi.Order.Payments.Any(p => (p.ProviderMode == "TEST" || p.ProviderMode == "Sandbox")))
            .GroupBy(oi => new { ProductId = oi.ProductId ?? 0, Name = !string.IsNullOrEmpty(oi.ProductNameSnapshot) ? oi.ProductNameSnapshot : (oi.Product != null ? oi.Product.Name : string.Empty), ImageUrl = oi.ImageSnapshot ?? (oi.Product != null ? oi.Product.ImageUrl : string.Empty) })
            .Select(g => new TopSellingProduct { ProductId = g.Key.ProductId, ProductName = g.Key.Name, ProductImageUrl = g.Key.ImageUrl, TotalQuantitySold = g.Sum(oi => oi.Quantity), TotalRevenue = g.Sum(oi => oi.Subtotal) })
            .OrderByDescending(p => p.TotalQuantitySold)
            .Take(10)
            .ToListAsync();
        var response = new ProductStatsResponse
        {
            TotalProducts = totalProducts,
            ActiveProducts = activeProducts,
            InactiveProducts = inactiveProducts,
            OutOfStockProducts = outOfStockProducts,
            LowStockProducts = lowStockProducts,
            TopSellingProducts = topSellingProducts
        };
        return response;
    }

    public async Task<List<RecentOrderResponse>> GetRecentOrders(int limit = 10)
    {
        var recentOrders = await _context.Orders.Include(o => o.User)
            .OrderByDescending(o => o.OrderDate)
            .Take(limit)
            .Select(o => new RecentOrderResponse { Id = o.Id, OrderNumber = o.OrderNumber, CustomerName = o.User!.Name, CustomerEmail = o.User.Email ?? string.Empty, Total = o.Total, Currency = o.Currency, Status = o.Status.ToString(), OrderDate = o.OrderDate })
            .ToListAsync();
        return recentOrders;
    }
}
