using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using be.Data;
using be.DTOs.Cart;
using be.Models;
using System.Security.Claims;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All cart endpoints require authentication
    public class CartController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(ApplicationDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/cart
        [HttpGet]
        public async Task<ActionResult<CartResponse>> GetCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var cartItems = await _context.CartItems
                    .Include(c => c.Product)
                    .Where(c => c.UserId == userId)
                    .Select(c => new CartItemResponse
                    {
                        Id = c.Id,
                        ProductId = c.ProductId,
                        ProductName = c.Product!.Name,
                        ProductPrice = c.Product.Price,
                        ProductImageUrl = c.Product.ImageUrl,
                        Quantity = c.Quantity,
                        Subtotal = c.Product.Price * c.Quantity,
                        IsOutOfStock = c.Product.Stock == 0,
                        ProductStock = c.Product.Stock,
                        CreatedAt = c.CreatedAt,
                        UpdatedAt = c.UpdatedAt
                    })
                    .ToListAsync();

                var total = cartItems.Sum(item => item.Subtotal);
                var totalItems = cartItems.Sum(item => item.Quantity);

                var response = new CartResponse
                {
                    Items = cartItems,
                    Total = total,
                    TotalItems = totalItems
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting cart");
                return StatusCode(500, new { message = "An error occurred while retrieving the cart" });
            }
        }

        // POST: api/cart
        [HttpPost]
        public async Task<ActionResult<CartItemResponse>> AddToCart([FromBody] AddToCartRequest request)
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

                // Check if product exists and is active
                var product = await _context.Products
                    .FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                // Check stock availability
                if (product.Stock < request.Quantity)
                {
                    return BadRequest(new { message = $"Only {product.Stock} items available in stock" });
                }

                // Check if item already exists in cart
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

                CartItem cartItem;

                if (existingCartItem != null)
                {
                    // Update quantity if item already exists
                    var newQuantity = existingCartItem.Quantity + request.Quantity;
                    
                    // Check stock availability for new total quantity
                    if (product.Stock < newQuantity)
                    {
                        return BadRequest(new { message = $"Only {product.Stock} items available in stock. You already have {existingCartItem.Quantity} in cart." });
                    }

                    existingCartItem.Quantity = newQuantity;
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                    cartItem = existingCartItem;
                }
                else
                {
                    // Create new cart item
                    cartItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                // Reload with product data
                await _context.Entry(cartItem)
                    .Reference(c => c.Product)
                    .LoadAsync();

                var response = new CartItemResponse
                {
                    Id = cartItem.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product!.Name,
                    ProductPrice = cartItem.Product.Price,
                    ProductImageUrl = cartItem.Product.ImageUrl,
                    Quantity = cartItem.Quantity,
                    Subtotal = cartItem.Product.Price * cartItem.Quantity,
                    IsOutOfStock = cartItem.Product.Stock == 0,
                    ProductStock = cartItem.Product.Stock,
                    CreatedAt = cartItem.CreatedAt,
                    UpdatedAt = cartItem.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to cart");
                return StatusCode(500, new { message = "An error occurred while adding item to cart" });
            }
        }

        // PUT: api/cart/{itemId}
        [HttpPut("{itemId}")]
        public async Task<ActionResult<CartItemResponse>> UpdateCartItem(int itemId, [FromBody] UpdateCartItemRequest request)
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

                var cartItem = await _context.CartItems
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return NotFound(new { message = "Cart item not found" });
                }

                // Check stock availability
                if (cartItem.Product!.Stock < request.Quantity)
                {
                    return BadRequest(new { message = $"Only {cartItem.Product.Stock} items available in stock" });
                }

                cartItem.Quantity = request.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var response = new CartItemResponse
                {
                    Id = cartItem.Id,
                    ProductId = cartItem.ProductId,
                    ProductName = cartItem.Product.Name,
                    ProductPrice = cartItem.Product.Price,
                    ProductImageUrl = cartItem.Product.ImageUrl,
                    Quantity = cartItem.Quantity,
                    Subtotal = cartItem.Product.Price * cartItem.Quantity,
                    IsOutOfStock = cartItem.Product.Stock == 0,
                    ProductStock = cartItem.Product.Stock,
                    CreatedAt = cartItem.CreatedAt,
                    UpdatedAt = cartItem.UpdatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cart item {ItemId}", itemId);
                return StatusCode(500, new { message = "An error occurred while updating cart item" });
            }
        }

        // DELETE: api/cart/{itemId}
        [HttpDelete("{itemId}")]
        public async Task<IActionResult> RemoveCartItem(int itemId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.Id == itemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return NotFound(new { message = "Cart item not found" });
                }

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Item removed from cart successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing cart item {ItemId}", itemId);
                return StatusCode(500, new { message = "An error occurred while removing cart item" });
            }
        }

        // DELETE: api/cart
        [HttpDelete]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var cartItems = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Cart cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing cart");
                return StatusCode(500, new { message = "An error occurred while clearing cart" });
            }
        }
    }
}

