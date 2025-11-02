using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using be.Data;
using be.DTOs.Order;
using be.Models;
using System.Security.Claims;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All order endpoints require authentication
    public class OrdersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(ApplicationDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/orders - Get user's orders
        [HttpGet]
        public async Task<ActionResult<List<OrderResponse>>> GetUserOrders()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.User)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .Select(o => new OrderResponse
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        UserId = o.UserId,
                        UserName = o.User!.Name,
                        UserEmail = o.User.Email ?? string.Empty,
                        OrderDate = o.OrderDate,
                        Total = o.Total,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        ShippingCity = o.ShippingCity,
                        ShippingPostalCode = o.ShippingPostalCode,
                        ShippingCountry = o.ShippingCountry,
                        Notes = o.Notes,
                        OrderItems = o.OrderItems.Select(oi => new OrderItemResponse
                        {
                            Id = oi.Id,
                            ProductId = oi.ProductId,
                            ProductName = oi.Product!.Name,
                            ProductImageUrl = oi.Product.ImageUrl,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            Subtotal = oi.Subtotal
                        }).ToList(),
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(orders);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user orders");
                return StatusCode(500, new { message = "An error occurred while retrieving orders" });
            }
        }

        // GET: api/orders/{id} - Get order details
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderResponse>> GetOrder(int id)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var isAdmin = User.IsInRole("Admin");

                var query = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.User)
                    .AsQueryable();

                // Non-admin users can only see their own orders
                if (!isAdmin)
                {
                    query = query.Where(o => o.UserId == userId);
                }

                var order = await query
                    .Where(o => o.Id == id)
                    .Select(o => new OrderResponse
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        UserId = o.UserId,
                        UserName = o.User!.Name,
                        UserEmail = o.User.Email ?? string.Empty,
                        OrderDate = o.OrderDate,
                        Total = o.Total,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        ShippingCity = o.ShippingCity,
                        ShippingPostalCode = o.ShippingPostalCode,
                        ShippingCountry = o.ShippingCountry,
                        Notes = o.Notes,
                        OrderItems = o.OrderItems.Select(oi => new OrderItemResponse
                        {
                            Id = oi.Id,
                            ProductId = oi.ProductId,
                            ProductName = oi.Product!.Name,
                            ProductImageUrl = oi.Product.ImageUrl,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            Subtotal = oi.Subtotal
                        }).ToList(),
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting order {OrderId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the order" });
            }
        }

        // POST: api/orders - Create order (checkout from cart)
        [HttpPost]
        public async Task<ActionResult<OrderResponse>> CreateOrder([FromBody] CreateOrderRequest request)
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

                // Get user's cart items
                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                if (cartItems.Count == 0)
                {
                    return BadRequest(new { message = "Cart is empty. Please add items to cart before checkout." });
                }

                // Validate stock and calculate total
                decimal total = 0;
                var orderItems = new List<OrderItem>();
                var errors = new List<string>();

                foreach (var cartItem in cartItems)
                {
                    var product = cartItem.Product;
                    if (product == null || !product.IsActive)
                    {
                        errors.Add($"Product {cartItem.ProductId} is no longer available");
                        continue;
                    }

                    if (product.Stock < cartItem.Quantity)
                    {
                        errors.Add($"Insufficient stock for {product.Name}. Available: {product.Stock}, Requested: {cartItem.Quantity}");
                        continue;
                    }

                    var subtotal = product.Price * cartItem.Quantity;
                    total += subtotal;

                    orderItems.Add(new OrderItem
                    {
                        ProductId = product.Id,
                        Quantity = cartItem.Quantity,
                        Price = product.Price, // Store price at time of order
                        Subtotal = subtotal
                    });

                    // Reduce stock
                    product.Stock -= cartItem.Quantity;
                    product.UpdatedAt = DateTime.UtcNow;
                }

                if (errors.Any())
                {
                    return BadRequest(new { message = "Some items are not available", errors });
                }

                // Generate unique order number
                var orderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

                // Create order
                var order = new Order
                {
                    OrderNumber = orderNumber,
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    Total = total,
                    Status = OrderStatus.Pending,
                    ShippingAddress = request.ShippingAddress,
                    ShippingCity = request.ShippingCity,
                    ShippingPostalCode = request.ShippingPostalCode,
                    ShippingCountry = request.ShippingCountry,
                    Notes = request.Notes,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    OrderItems = orderItems
                };

                _context.Orders.Add(order);

                // Clear cart after order is created
                _context.CartItems.RemoveRange(cartItems);

                await _context.SaveChangesAsync();

                // Reload order with related data
                await _context.Entry(order)
                    .Collection(o => o.OrderItems)
                    .Query()
                    .Include(oi => oi.Product)
                    .LoadAsync();

                await _context.Entry(order)
                    .Reference(o => o.User)
                    .LoadAsync();

                var response = new OrderResponse
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    UserId = order.UserId,
                    UserName = order.User!.Name,
                    UserEmail = order.User.Email ?? string.Empty,
                    OrderDate = order.OrderDate,
                    Total = order.Total,
                    Status = order.Status.ToString(),
                    ShippingAddress = order.ShippingAddress,
                    ShippingCity = order.ShippingCity,
                    ShippingPostalCode = order.ShippingPostalCode,
                    ShippingCountry = order.ShippingCountry,
                    Notes = order.Notes,
                    OrderItems = order.OrderItems.Select(oi => new OrderItemResponse
                    {
                        Id = oi.Id,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product!.Name,
                        ProductImageUrl = oi.Product.ImageUrl,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        Subtotal = oi.Subtotal
                    }).ToList(),
                    CreatedAt = order.CreatedAt,
                    UpdatedAt = order.UpdatedAt
                };

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new { message = "An error occurred while creating the order" });
            }
        }

        // PUT: api/orders/{id}/status - Update order status (Admin only)
        [HttpPut("{id}/status")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<OrderResponse>> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.User)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                // Parse status string to enum
                if (!Enum.TryParse<OrderStatus>(request.Status, true, out var newStatus))
                {
                    return BadRequest(new { message = "Invalid status. Must be one of: Pending, Processing, Shipped, Delivered, Cancelled" });
                }

                // If cancelling order, restore stock
                if (newStatus == OrderStatus.Cancelled && order.Status != OrderStatus.Cancelled)
                {
                    foreach (var orderItem in order.OrderItems)
                    {
                        var product = await _context.Products.FindAsync(orderItem.ProductId);
                        if (product != null)
                        {
                            product.Stock += orderItem.Quantity;
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }
                // If cancelling a cancelled order, reduce stock again
                else if (order.Status == OrderStatus.Cancelled && newStatus != OrderStatus.Cancelled)
                {
                    foreach (var orderItem in order.OrderItems)
                    {
                        var product = await _context.Products.FindAsync(orderItem.ProductId);
                        if (product != null && product.Stock < orderItem.Quantity)
                        {
                            return BadRequest(new { message = $"Cannot change status. Insufficient stock for {product.Name}" });
                        }
                        if (product != null)
                        {
                            product.Stock -= orderItem.Quantity;
                            product.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                order.Status = newStatus;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new OrderResponse
                {
                    Id = order.Id,
                    OrderNumber = order.OrderNumber,
                    UserId = order.UserId,
                    UserName = order.User!.Name,
                    UserEmail = order.User.Email ?? string.Empty,
                    OrderDate = order.OrderDate,
                    Total = order.Total,
                    Status = order.Status.ToString(),
                    ShippingAddress = order.ShippingAddress,
                    ShippingCity = order.ShippingCity,
                    ShippingPostalCode = order.ShippingPostalCode,
                    ShippingCountry = order.ShippingCountry,
                    Notes = order.Notes,
                    OrderItems = order.OrderItems.Select(oi => new OrderItemResponse
                    {
                        Id = oi.Id,
                        ProductId = oi.ProductId,
                        ProductName = oi.Product!.Name,
                        ProductImageUrl = oi.Product.ImageUrl,
                        Quantity = oi.Quantity,
                        Price = oi.Price,
                        Subtotal = oi.Subtotal
                    }).ToList(),
                    CreatedAt = order.CreatedAt,
                    UpdatedAt = order.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating order status {OrderId}", id);
                return StatusCode(500, new { message = "An error occurred while updating order status" });
            }
        }

        // GET: api/orders/admin/all - Get all orders (Admin only)
        [HttpGet("admin/all")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<List<OrderResponse>>> GetAllOrders(
            [FromQuery] string? status = null,
            [FromQuery] string? search = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.User)
                    .AsQueryable();

                // Filter by status
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<OrderStatus>(status, true, out var orderStatus))
                    {
                        query = query.Where(o => o.Status == orderStatus);
                    }
                }

                // Search by order number or user name/email
                if (!string.IsNullOrEmpty(search))
                {
                    query = query.Where(o =>
                        o.OrderNumber.Contains(search) ||
                        o.User!.Name.Contains(search) ||
                        o.User!.Email!.Contains(search));
                }

                // Get total count before pagination
                var totalCount = await query.CountAsync();

                // Apply pagination
                var orders = await query
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(o => new OrderResponse
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        UserId = o.UserId,
                        UserName = o.User!.Name,
                        UserEmail = o.User.Email ?? string.Empty,
                        OrderDate = o.OrderDate,
                        Total = o.Total,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        ShippingCity = o.ShippingCity,
                        ShippingPostalCode = o.ShippingPostalCode,
                        ShippingCountry = o.ShippingCountry,
                        Notes = o.Notes,
                        OrderItems = o.OrderItems.Select(oi => new OrderItemResponse
                        {
                            Id = oi.Id,
                            ProductId = oi.ProductId,
                            ProductName = oi.Product!.Name,
                            ProductImageUrl = oi.Product.ImageUrl,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            Subtotal = oi.Subtotal
                        }).ToList(),
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt
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
                _logger.LogError(ex, "Error getting all orders");
                return StatusCode(500, new { message = "An error occurred while retrieving orders" });
            }
        }

        // GET: api/orders/admin/{id} - Get order details (Admin only)
        [HttpGet("admin/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<OrderResponse>> GetAdminOrder(int id)
        {
            try
            {
                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                    .Include(o => o.User)
                    .Where(o => o.Id == id)
                    .Select(o => new OrderResponse
                    {
                        Id = o.Id,
                        OrderNumber = o.OrderNumber,
                        UserId = o.UserId,
                        UserName = o.User!.Name,
                        UserEmail = o.User.Email ?? string.Empty,
                        OrderDate = o.OrderDate,
                        Total = o.Total,
                        Status = o.Status.ToString(),
                        ShippingAddress = o.ShippingAddress,
                        ShippingCity = o.ShippingCity,
                        ShippingPostalCode = o.ShippingPostalCode,
                        ShippingCountry = o.ShippingCountry,
                        Notes = o.Notes,
                        OrderItems = o.OrderItems.Select(oi => new OrderItemResponse
                        {
                            Id = oi.Id,
                            ProductId = oi.ProductId,
                            ProductName = oi.Product!.Name,
                            ProductImageUrl = oi.Product.ImageUrl,
                            Quantity = oi.Quantity,
                            Price = oi.Price,
                            Subtotal = oi.Subtotal
                        }).ToList(),
                        CreatedAt = o.CreatedAt,
                        UpdatedAt = o.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin order {OrderId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the order" });
            }
        }
    }
}

