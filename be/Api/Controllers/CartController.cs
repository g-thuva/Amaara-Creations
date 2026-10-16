using be.Application.Features.Cart;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace be.Api.Controllers;
[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController(CartService service) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCart(CancellationToken cancellationToken)
    {
        return Ok(await service.GetCart(cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<CartItemResponse>> AddToCart([FromBody] AddToCartRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.AddToCart(request, cancellationToken));
    }

    [HttpPut("{itemId:int}")]
    public async Task<ActionResult<CartItemResponse>> UpdateCartItem(int itemId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        return Ok(await service.UpdateCartItem(itemId, request, cancellationToken));
    }

    [HttpDelete("{itemId:int}")]
    public async Task<IActionResult> RemoveCartItem(int itemId, CancellationToken cancellationToken)
    {
        await service.RemoveCartItem(itemId, cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        await service.ClearCart(cancellationToken);
        return NoContent();
    }
}
