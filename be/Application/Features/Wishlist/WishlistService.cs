using be.Domain.Cart;
using be.Domain.Wishlist;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Domain.Common;

namespace be.Application.Features.Wishlist;
public class WishlistService
{
    private readonly IApplicationDbContext _context;
    public WishlistService(IApplicationDbContext context, IRequestActor actor)
    {
        _context = context;
        _actor = actor;
    }

    public async Task<WishlistResponse> GetWishlist()
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("WISHLIST_UNAUTHORIZED", "User not found", 401);
        }

        var wishlistItems = await _context.WishlistItems.Include(w => w.Product)
            .Where(w => w.UserId == userId)
            .OrderByDescending(w => w.CreatedAt)
            .Select(w => new WishlistItemResponse { Id = w.Id, ProductId = w.ProductId, ProductName = w.Product!.Name, ProductPrice = w.Product.Price, Currency = w.Product.Currency, ProductImageUrl = w.Product.ImageUrl, ProductCategory = w.Product.Category, IsOutOfStock = w.Product.Stock == 0, ProductStock = w.Product.Stock, AddedAt = w.CreatedAt })
            .ToListAsync();
        var response = new WishlistResponse
        {
            Items = wishlistItems,
            TotalItems = wishlistItems.Count
        };
        return response;
    }

    public async Task<object> AddToWishlist(AddToWishlistRequest request)
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("WISHLIST_UNAUTHORIZED", "User not found", 401);
        }

        // Check if product exists and is active
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == request.ProductId && p.IsActive);
        if (product == null)
        {
            throw new CommerceException("WISHLIST_NOT_FOUND", "Product not found", 404);
        }

        // Check if item already exists in wishlist
        var existingWishlistItem = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == request.ProductId);
        if (existingWishlistItem != null)
        {
            // Already in wishlist - return existing item
            await _context.Entry(existingWishlistItem).Reference(w => w.Product).LoadAsync();
            var existingResponse = new WishlistItemResponse
            {
                Id = existingWishlistItem.Id,
                ProductId = existingWishlistItem.ProductId,
                ProductName = existingWishlistItem.Product!.Name,
                ProductPrice = existingWishlistItem.Product.Price,
                Currency = existingWishlistItem.Product.Currency,
                ProductImageUrl = existingWishlistItem.Product.ImageUrl,
                ProductCategory = existingWishlistItem.Product.Category,
                IsOutOfStock = existingWishlistItem.Product.Stock == 0,
                ProductStock = existingWishlistItem.Product.Stock,
                AddedAt = existingWishlistItem.CreatedAt
            };
            return new
            {
                message = "Product already in wishlist",
                item = existingResponse
            };
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
        await _context.Entry(wishlistItem).Reference(w => w.Product).LoadAsync();
        var response = new WishlistItemResponse
        {
            Id = wishlistItem.Id,
            ProductId = wishlistItem.ProductId,
            ProductName = wishlistItem.Product!.Name,
            ProductPrice = wishlistItem.Product.Price,
            Currency = wishlistItem.Product.Currency,
            ProductImageUrl = wishlistItem.Product.ImageUrl,
            ProductCategory = wishlistItem.Product.Category,
            IsOutOfStock = wishlistItem.Product.Stock == 0,
            ProductStock = wishlistItem.Product.Stock,
            AddedAt = wishlistItem.CreatedAt
        };
        return response;
    }

    public async Task<object?> RemoveFromWishlist(int productId)
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("WISHLIST_UNAUTHORIZED", "User not found", 401);
        }

        var wishlistItem = await _context.WishlistItems.FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (wishlistItem == null)
        {
            throw new CommerceException("WISHLIST_NOT_FOUND", "Product not found in wishlist", 404);
        }

        _context.WishlistItems.Remove(wishlistItem);
        await _context.SaveChangesAsync();
        return new
        {
            message = "Product removed from wishlist successfully"
        };
    }

    public async Task<object?> AddWishlistItemToCart(int productId, int quantity = 1)
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("WISHLIST_UNAUTHORIZED", "User not found", 401);
        }

        // Check if item exists in wishlist
        var wishlistItem = await _context.WishlistItems.Include(w => w.Product).FirstOrDefaultAsync(w => w.UserId == userId && w.ProductId == productId);
        if (wishlistItem == null)
        {
            throw new CommerceException("WISHLIST_NOT_FOUND", "Product not found in wishlist", 404);
        }

        var product = wishlistItem.Product;
        if (product == null || !product.IsActive)
        {
            throw new CommerceException("WISHLIST_REQUEST_INVALID", "Product is no longer available", 400);
        }

        // Check stock availability
        if (product.Stock < quantity)
        {
            throw new CommerceException("WISHLIST_REQUEST_INVALID", $"Only {product.Stock} items available in stock", 400);
        }

        // Check if item already exists in cart
        var existingCartItem = await _context.CartItems.FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == productId);
        CartItem cartItem;
        if (existingCartItem != null)
        {
            // Update quantity if item already exists
            var newQuantity = existingCartItem.Quantity + quantity;
            // Check stock availability for new total quantity
            if (product.Stock < newQuantity)
            {
                throw new CommerceException("WISHLIST_REQUEST_INVALID", $"Only {product.Stock} items available in stock. You already have {existingCartItem.Quantity} in cart.", 400);
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
        return new
        {
            message = "Product added to cart successfully"
        };
    }

    public async Task<object?> ClearWishlist()
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            throw new CommerceException("WISHLIST_UNAUTHORIZED", "User not found", 401);
        }

        var wishlistItems = await _context.WishlistItems.Where(w => w.UserId == userId).ToListAsync();
        _context.WishlistItems.RemoveRange(wishlistItems);
        await _context.SaveChangesAsync();
        return new
        {
            message = "Wishlist cleared successfully"
        };
    }

    private readonly IRequestActor _actor;
}
