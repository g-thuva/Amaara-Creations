using be.Application.Features.Wishlist;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/wishlist")]
[Authorize] // All wishlist endpoints require authentication
public class WishlistController(WishlistService service) : ControllerBase
{
    // GET: api/wishlist - Get user's wishlist
    [HttpGet]
    public async Task<ActionResult<WishlistResponse>> GetWishlist()
    {
        return Ok(await service.GetWishlist());
    }

    // POST: api/wishlist - Add product to wishlist
    [HttpPost]
    public async Task<ActionResult<WishlistItemResponse>> AddToWishlist([FromBody] AddToWishlistRequest request)
    {
        return Ok(await service.AddToWishlist(request));
    }

    // DELETE: api/wishlist/{productId} - Remove from wishlist
    [HttpDelete("{productId}")]
    public async Task<IActionResult> RemoveFromWishlist(int productId)
    {
        return Ok(await service.RemoveFromWishlist(productId));
    }

    // POST: api/wishlist/{productId}/cart - Add wishlist item to cart
    [HttpPost("{productId}/cart")]
    public async Task<ActionResult> AddWishlistItemToCart(int productId, [FromQuery] int quantity = 1)
    {
        return Ok(await service.AddWishlistItemToCart(productId, quantity));
    }

    // DELETE: api/wishlist - Clear entire wishlist
    [HttpDelete]
    public async Task<IActionResult> ClearWishlist()
    {
        return Ok(await service.ClearWishlist());
    }
}
