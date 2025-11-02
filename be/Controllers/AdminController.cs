using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using be.Data;
using be.DTOs.Admin;
using be.Models;
using System.Security.Claims;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Roles = "Admin")] // All admin endpoints require Admin role
    public class AdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminController> logger)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: api/admin/dashboard - Get dashboard statistics
        [HttpGet("dashboard")]
        public async Task<ActionResult<DashboardStatsResponse>> GetDashboardStats()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var endOfToday = today.AddDays(1);

                // Total Revenue (excluding cancelled orders)
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                // Revenue Today
                var revenueToday = await _context.Orders
                    .Where(o => o.OrderDate >= today && o.OrderDate < endOfToday && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                // Revenue This Month
                var revenueThisMonth = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                // Total Orders
                var totalOrders = await _context.Orders.CountAsync();

                // Orders Today
                var ordersToday = await _context.Orders
                    .Where(o => o.OrderDate >= today && o.OrderDate < endOfToday)
                    .CountAsync();

                // Orders This Month
                var ordersThisMonth = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth)
                    .CountAsync();

                // Total Customers (users with role "Customer")
                var totalCustomers = await _userManager.GetUsersInRoleAsync("Customer");
                var customerCount = totalCustomers.Count;

                // Total Products
                var totalProducts = await _context.Products.CountAsync();

                // Average Rating
                var averageRating = await _context.Reviews
                    .Select(r => (double?)r.Rating)
                    .AverageAsync() ?? 0.0;

                // Order Status Breakdown
                var pendingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending)
                    .CountAsync();

                var processingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Processing)
                    .CountAsync();

                var shippedOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Shipped)
                    .CountAsync();

                var deliveredOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .CountAsync();

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

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting dashboard statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving dashboard statistics" });
            }
        }

        // GET: api/admin/dashboard/revenue - Get revenue statistics
        [HttpGet("dashboard/revenue")]
        public async Task<ActionResult<RevenueStatsResponse>> GetRevenueStats()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var endOfToday = today.AddDays(1);
                var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var thisYear = new DateTime(DateTime.UtcNow.Year, 1, 1);

                // Total Revenue
                var totalRevenue = await _context.Orders
                    .Where(o => o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                // Revenue Today
                var revenueToday = await _context.Orders
                    .Where(o => o.OrderDate >= today && o.OrderDate < endOfToday && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                // Revenue This Week
                var revenueThisWeek = await _context.Orders
                    .Where(o => o.OrderDate >= thisWeek && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                // Revenue This Month
                var revenueThisMonth = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                // Revenue This Year
                var revenueThisYear = await _context.Orders
                    .Where(o => o.OrderDate >= thisYear && o.Status != OrderStatus.Cancelled)
                    .SumAsync(o => (decimal?)o.Total) ?? 0;

                // Monthly Revenue for last 12 months
                var monthlyRevenues = new List<MonthlyRevenue>();
                for (int i = 11; i >= 0; i--)
                {
                    var monthStart = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1).AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1);

                    var monthRevenue = await _context.Orders
                        .Where(o => o.OrderDate >= monthStart && o.OrderDate < monthEnd && o.Status != OrderStatus.Cancelled)
                        .SumAsync(o => (decimal?)o.Total) ?? 0;

                    monthlyRevenues.Add(new MonthlyRevenue
                    {
                        Month = monthStart.ToString("MMMM"),
                        Year = monthStart.Year,
                        Revenue = monthRevenue
                    });
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting revenue statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving revenue statistics" });
            }
        }

        // GET: api/admin/dashboard/orders - Get order statistics
        [HttpGet("dashboard/orders")]
        public async Task<ActionResult<OrderStatsResponse>> GetOrderStats()
        {
            try
            {
                var today = DateTime.UtcNow.Date;
                var endOfToday = today.AddDays(1);
                var thisWeek = today.AddDays(-(int)today.DayOfWeek);
                var thisMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1);
                var thisYear = new DateTime(DateTime.UtcNow.Year, 1, 1);

                // Total Orders
                var totalOrders = await _context.Orders.CountAsync();

                // Orders Today
                var ordersToday = await _context.Orders
                    .Where(o => o.OrderDate >= today && o.OrderDate < endOfToday)
                    .CountAsync();

                // Orders This Week
                var ordersThisWeek = await _context.Orders
                    .Where(o => o.OrderDate >= thisWeek)
                    .CountAsync();

                // Orders This Month
                var ordersThisMonth = await _context.Orders
                    .Where(o => o.OrderDate >= thisMonth)
                    .CountAsync();

                // Orders This Year
                var ordersThisYear = await _context.Orders
                    .Where(o => o.OrderDate >= thisYear)
                    .CountAsync();

                // Order Status Breakdown
                var pendingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Pending)
                    .CountAsync();

                var processingOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Processing)
                    .CountAsync();

                var shippedOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Shipped)
                    .CountAsync();

                var deliveredOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Delivered)
                    .CountAsync();

                var cancelledOrders = await _context.Orders
                    .Where(o => o.Status == OrderStatus.Cancelled)
                    .CountAsync();

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

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving order statistics" });
            }
        }

        // GET: api/admin/dashboard/products - Get product statistics
        [HttpGet("dashboard/products")]
        public async Task<ActionResult<ProductStatsResponse>> GetProductStats()
        {
            try
            {
                // Total Products
                var totalProducts = await _context.Products.CountAsync();

                // Active Products
                var activeProducts = await _context.Products
                    .Where(p => p.IsActive)
                    .CountAsync();

                // Inactive Products
                var inactiveProducts = totalProducts - activeProducts;

                // Out of Stock Products
                var outOfStockProducts = await _context.Products
                    .Where(p => p.Stock == 0)
                    .CountAsync();

                // Low Stock Products (Stock < 10)
                var lowStockProducts = await _context.Products
                    .Where(p => p.Stock > 0 && p.Stock < 10)
                    .CountAsync();

                // Top Selling Products (by quantity sold)
                var topSellingProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                    .Where(oi => oi.Order!.Status != OrderStatus.Cancelled)
                    .GroupBy(oi => new { oi.ProductId, oi.Product!.Name, oi.Product.ImageUrl })
                    .Select(g => new TopSellingProduct
                    {
                        ProductId = g.Key.ProductId,
                        ProductName = g.Key.Name,
                        ProductImageUrl = g.Key.ImageUrl,
                        TotalQuantitySold = g.Sum(oi => oi.Quantity),
                        TotalRevenue = g.Sum(oi => oi.Subtotal)
                    })
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product statistics");
                return StatusCode(500, new { message = "An error occurred while retrieving product statistics" });
            }
        }

        // GET: api/admin/dashboard/recent-orders - Get recent orders
        [HttpGet("dashboard/recent-orders")]
        public async Task<ActionResult<List<RecentOrderResponse>>> GetRecentOrders([FromQuery] int limit = 10)
        {
            try
            {
                var recentOrders = await _context.Orders
                    .Include(o => o.User)
                    .OrderByDescending(o => o.OrderDate)
                    .Take(limit)
                    .Select(o => new RecentOrderResponse
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        CustomerName = o.User!.Name,
                        CustomerEmail = o.User.Email ?? string.Empty,
                        Total = o.Total,
                        Status = o.Status.ToString(),
                        OrderDate = o.OrderDate
                    })
                    .ToListAsync();

                return Ok(recentOrders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent orders");
                return StatusCode(500, new { message = "An error occurred while retrieving recent orders" });
            }
        }

        // GET: api/admin/customers - Get all customers
        [HttpGet("customers")]
        public async Task<ActionResult> GetAllCustomers(
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var customersRole = await _roleManager.FindByNameAsync("Customer");
                if (customersRole == null)
                {
                    return Ok(new { Customers = new List<CustomerResponse>(), TotalCount = 0, PageNumber = 1, PageSize = pageSize, TotalPages = 0 });
                }

                var customerUserIds = await _context.UserRoles
                    .Where(ur => ur.RoleId == customersRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                var query = _context.Users
                    .Where(u => customerUserIds.Contains(u.Id))
                    .AsQueryable();

                // Search by name, email, or phone
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(u =>
                        u.Name.Contains(search) ||
                        u.Email!.Contains(search) ||
                        (u.Phone != null && u.Phone.Contains(search)));
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var customers = await query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new CustomerResponse
                    {
                        Id = u.Id,
                        Name = u.Name,
                        Email = u.Email ?? string.Empty,
                        Phone = u.Phone,
                        Address = u.Address,
                        AvatarUrl = u.AvatarUrl,
                        CreatedAt = u.CreatedAt,
                        UpdatedAt = u.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Customers = customers,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting all customers");
                return StatusCode(500, new { message = "An error occurred while retrieving customers" });
            }
        }

        // GET: api/admin/customers/{id} - Get customer details
        [HttpGet("customers/{id}")]
        public async Task<ActionResult<CustomerDetailResponse>> GetCustomerDetails(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Customer not found" });
                }

                // Check if user is a customer
                var roles = await _userManager.GetRolesAsync(user);
                if (!roles.Contains("Customer"))
                {
                    return BadRequest(new { message = "User is not a customer" });
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

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer details {CustomerId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving customer details" });
            }
        }

        // GET: api/admin/customers/{id}/orders - Get customer orders
        [HttpGet("customers/{id}/orders")]
        public async Task<ActionResult> GetCustomerOrders(string id, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Customer not found" });
                }

                var query = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Where(o => o.UserId == id)
                    .AsQueryable();

                var totalCount = await query.CountAsync();

                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.OrderDate,
                        Total = o.Total,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        ShippingCity = o.ShippingCity,
                        TotalItems = o.OrderItems.Sum(oi => oi.Quantity)
                    })
                    .ToListAsync();

                return Ok(new
                {
                    Orders = orders,
                    TotalCount = totalCount,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer orders {CustomerId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving customer orders" });
            }
        }

        // GET: api/admin/customers/{id}/stats - Get customer statistics
        [HttpGet("customers/{id}/stats")]
        public async Task<ActionResult<CustomerStatsResponse>> GetCustomerStats(string id)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(id);
                if (user == null)
                {
                    return NotFound(new { message = "Customer not found" });
                }

                var stats = await GetCustomerStatistics(id);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting customer statistics {CustomerId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving customer statistics" });
            }
        }

        // Helper method to get customer statistics
        private async Task<CustomerStatsResponse> GetCustomerStatistics(string userId)
        {
            var totalOrders = await _context.Orders
                .Where(o => o.UserId == userId)
                .CountAsync();

            var pendingOrders = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.Pending)
                .CountAsync();

            var processingOrders = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.Processing)
                .CountAsync();

            var shippedOrders = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.Shipped)
                .CountAsync();

            var deliveredOrders = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.Delivered)
                .CountAsync();

            var cancelledOrders = await _context.Orders
                .Where(o => o.UserId == userId && o.Status == OrderStatus.Cancelled)
                .CountAsync();

            var totalSpent = await _context.Orders
                .Where(o => o.UserId == userId && o.Status != OrderStatus.Cancelled)
                .SumAsync(o => (decimal?)o.Total) ?? 0;

            var totalCartItems = await _context.CartItems
                .Where(c => c.UserId == userId)
                .CountAsync();

            var totalWishlistItems = await _context.WishlistItems
                .Where(w => w.UserId == userId)
                .CountAsync();

            var totalReviews = await _context.Reviews
                .Where(r => r.UserId == userId)
                .CountAsync();

            var lastOrderDate = await _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .Select(o => (DateTime?)o.OrderDate)
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
}

