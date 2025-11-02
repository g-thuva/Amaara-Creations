using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using be.Data;
using be.DTOs.Wishlist;
using be.Models;
using System.Security.Claims;

namespace be.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // All wishlist endpoints require authentication
    public class WishlistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WishlistController> _logger;

        public WishlistController(ApplicationDbContext context, ILogger<WishlistController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/wishlist - Get user's wishlist
        [HttpGet]
        public async Task<ActionResult<WishlistResponse>> GetWishlist()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var wishlistItems = await _context.WishlistItems
                    .Include(w => w.Product)
                    .Where(w => w.UserId == userId)
                    .OrderByDescending(w => w.CreatedAt)
                    .Select(w => new WishlistItemResponse
                    {
                        Id = w.Id,
                        ProductId = w.ProductId,
                        ProductName = w.Product!.Name,
                        ProductPrice = w.Product.Price,
                        ProductImageUrl = w.Product.ImageUrl,
                        ProductCategory = w.Product.Category,
                        IsOutOfStock = w.Product.Stock == 0,
                        ProductStock = w.Product.Stock,
                        AddedAt = w.CreatedAt
                    })
                    .ToListAsync();

                var response = new WishlistResponse
                {
                    Items = wishlistItems,
                    TotalItems = wishlistItems.Count
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting wishlist");
                return StatusCode(500, new { message = "An error occurred while retrieving the wishlist" });
            }
        }

        // POST: api/wishlist - Add product to wishlist
        [HttpPost]
        public async Task<ActionResult<WishlistItemResponse>> AddToWishlist([FromBody] AddToWishlistRequest request)
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

                // Check if item already exists in wishlist
                var existingWishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == request.ProductId);

                if (existingWishlistItem != null)
                {
                    // Already in wishlist - return existing item
                    await _context.Entry(existingWishlistItem)
                        .Reference(w => w.Product)
                        .LoadAsync();

                    var existingResponse = new WishlistItemResponse
                    {
                        Id = existingWishlistItem.Id,
                        ProductId = existingWishlistItem.ProductId,
                        ProductName = existingWishlistItem.Product!.Name,
                        ProductPrice = existingWishlistItem.Product.Price,
                        ProductImageUrl = existingWishlistItem.Product.ImageUrl,
                        ProductCategory = existingWishlistItem.Product.Category,
                        IsOutOfStock = existingWishlistItem.Product.Stock == 0,
                        ProductStock = existingWishlistItem.Product.Stock,
                        AddedAt = existingWishlistItem.CreatedAt
                    };

                    return Ok(new { message = "Product already in wishlist", item = existingResponse });
                }

                // Create new wishlist item
                var wishlistItem = new WishlistItem
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WishlistItems.Add(wishlistItem);
                await _context.SaveChangesAsync();

                // Reload with product data
                await _context.Entry(wishlistItem)
                    .Reference(w => w.Product)
                    .LoadAsync();

                var response = new WishlistItemResponse
                {
                    Id = wishlistItem.Id,
                    ProductId = wishlistItem.ProductId,
                    ProductName = wishlistItem.Product!.Name,
                    ProductPrice = wishlistItem.Product.Price,
                    ProductImageUrl = wishlistItem.Product.ImageUrl,
                    ProductCategory = wishlistItem.Product.Category,
                    IsOutOfStock = wishlistItem.Product.Stock == 0,
                    ProductStock = wishlistItem.Product.Stock,
                    AddedAt = wishlistItem.CreatedAt
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding to wishlist");
                return StatusCode(500, new { message = "An error occurred while adding item to wishlist" });
            }
        }

        // DELETE: api/wishlist/{productId} - Remove from wishlist
        [HttpDelete("{productId}")]
        public async Task<IActionResult> RemoveFromWishlist(int productId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var wishlistItem = await _context.WishlistItems
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (wishlistItem == null)
                {
                    return NotFound(new { message = "Product not found in wishlist" });
                }

                _context.WishlistItems.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Product removed from wishlist successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing from wishlist {ProductId}", productId);
                return StatusCode(500, new { message = "An error occurred while removing item from wishlist" });
            }
        }

        // POST: api/wishlist/{productId}/cart - Add wishlist item to cart
        [HttpPost("{productId}/cart")]
        public async Task<ActionResult> AddWishlistItemToCart(int productId, [FromQuery] int quantity = 1)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                // Check if item exists in wishlist
                var wishlistItem = await _context.WishlistItems
                    .Include(w => w.Product)
                    .FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);

                if (wishlistItem == null)
                {
                    return NotFound(new { message = "Product not found in wishlist" });
                }

                var product = wishlistItem.Product;
                if (product == null || !product.IsActive)
                {
                    return BadRequest(new { message = "Product is no longer available" });
                }

                // Check stock availability
                if (product.Stock < quantity)
                {
                    return BadRequest(new { message = $"Only {product.Stock} items available in stock" });
                }

                // Check if item already exists in cart
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);

                CartItem cartItem;

                if (existingCartItem != null)
                {
                    // Update quantity if item already exists
                    var newQuantity = existingCartItem.Quantity + quantity;
                    
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
                        ProductId = productId,
                        Quantity = quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                return Ok(new { message = "Product added to cart successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding wishlist item to cart {ProductId}", productId);
                return StatusCode(500, new { message = "An error occurred while adding item to cart" });
            }
        }

        // DELETE: api/wishlist - Clear entire wishlist
        [HttpDelete]
        public async Task<IActionResult> ClearWishlist()
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (userId == null)
                {
                    return Unauthorized(new { message = "User not found" });
                }

                var wishlistItems = await _context.WishlistItems
                    .Where(w => w.UserId == userId)
                    .ToListAsync();

                _context.WishlistItems.RemoveRange(wishlistItems);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Wishlist cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error clearing wishlist");
                return StatusCode(500, new { message = "An error occurred while clearing wishlist" });
            }
        }
    }
}

