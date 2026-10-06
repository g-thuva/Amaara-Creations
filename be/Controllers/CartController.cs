using System.Security.Claims;
using be.Data;
using be.DTOs.Cart;
using be.DTOs.CustomBuilder;
using be.Models;
using be.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController]
[Route("api/v1/cart")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ICustomStickerPricingService _pricing;

    public CartController(ApplicationDbContext context, ICustomStickerPricingService pricing)
    {
        _context = context;
        _pricing = pricing;
    }

    [HttpGet]
    public async Task<ActionResult<CartResponse>> GetCart(CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null) return Unauthorized();
        var items = await CartQuery().Where(item => item.UserId == userId).AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync(cancellationToken);
        return Ok(ToCart(items));
    }

    [HttpPost]
    public async Task<ActionResult<CartItemResponse>> AddToCart([FromBody] AddToCartRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null) return Unauthorized();
        var product = await _context.Products.FirstOrDefaultAsync(item => item.Id == request.ProductId && item.IsActive, cancellationToken);
        if (product == null) return NotFound(new { message = "Product not found" });
        var existing = await _context.CartItems.FirstOrDefaultAsync(item => item.UserId == userId && item.ProductId == request.ProductId, cancellationToken);
        var quantity = (existing?.Quantity ?? 0) + request.Quantity;
        if (product.Stock < quantity) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["quantity"] = [$"Only {product.Stock} items are available."] }));
        var cartItem = existing ?? new CartItem { UserId = userId, ProductId = product.Id, ItemType = CartItemType.Product, CreatedAt = DateTime.UtcNow };
        cartItem.Quantity = quantity; cartItem.UnitPriceSnapshot = product.Price; cartItem.UpdatedAt = DateTime.UtcNow;
        if (existing == null) _context.CartItems.Add(cartItem);
        await _context.SaveChangesAsync(cancellationToken);
        cartItem.Product = product;
        return Ok(ToItem(cartItem));
    }

    [HttpPut("{itemId:int}")]
    public async Task<ActionResult<CartItemResponse>> UpdateCartItem(int itemId, [FromBody] UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null) return Unauthorized();
        var item = await CartQuery().FirstOrDefaultAsync(value => value.Id == itemId && value.UserId == userId, cancellationToken);
        if (item == null) return NotFound();
        if (item.ItemType == CartItemType.Product)
        {
            if (item.Product == null || item.Product.Stock < request.Quantity) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["quantity"] = [$"Only {item.Product?.Stock ?? 0} items are available."] }));
            item.Quantity = request.Quantity; item.UnitPriceSnapshot = item.Product.Price;
        }
        else
        {
            if (item.CustomDesign == null) return Conflict(new ProblemDetails { Title = "Custom design is unavailable", Status = 409 });
            var configuration = await ActiveConfiguration(cancellationToken);
            if (configuration == null) return Problem(statusCode: 503, title: "Custom builder is not configured");
            var requestQuote = QuoteRequest(item.CustomDesign, request.Quantity);
            try
            {
                var quote = _pricing.Quote(configuration, requestQuote);
                item.Quantity = quote.Quantity; item.UnitPriceSnapshot = quote.UnitPrice; item.BuilderConfigurationVersionId = configuration.Id;
                item.CustomDesign.Quantity = quote.Quantity; item.CustomDesign.UnitPrice = quote.UnitPrice; item.CustomDesign.CalculatedPrice = quote.Subtotal;
                item.CustomDesign.PricingRuleVersion = quote.PricingVersion; item.CustomDesign.BuilderConfigurationVersionId = configuration.Id;
                item.CustomDesign.QuotedAt = quote.QuotedAt; item.CustomDesign.UpdatedAt = DateTime.UtcNow;
            }
            catch (CustomBuilderValidationException exception) { return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { ["quantity"] = exception.Errors.ToArray() })); }
        }
        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return Ok(ToItem(item));
    }

    [HttpDelete("{itemId:int}")]
    public async Task<IActionResult> RemoveCartItem(int itemId, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null) return Unauthorized();
        var item = await _context.CartItems.Include(value => value.CustomDesign).FirstOrDefaultAsync(value => value.Id == itemId && value.UserId == userId, cancellationToken);
        if (item == null) return NotFound();
        if (item.CustomDesign != null && item.CustomDesign.Status == CustomDesignStatus.InCart) item.CustomDesign.Status = CustomDesignStatus.Ready;
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ClearCart(CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null) return Unauthorized();
        var items = await _context.CartItems.Include(item => item.CustomDesign).Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        foreach (var item in items.Where(item => item.CustomDesign?.Status == CustomDesignStatus.InCart)) item.CustomDesign!.Status = CustomDesignStatus.Ready;
        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private string? UserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

    private IQueryable<CartItem> CartQuery() => _context.CartItems
        .Include(item => item.Product)
        .Include(item => item.CustomDesign)!.ThenInclude(design => design!.BuilderConfigurationVersion)!.ThenInclude(version => version!.Options);

    private async Task<CustomBuilderConfigurationVersion?> ActiveConfiguration(CancellationToken cancellationToken) => await _context.CustomBuilderConfigurationVersions
        .Include(version => version.Options).Where(version => version.Status == BuilderVersionStatus.Published)
        .OrderByDescending(version => version.PublishedAt).ThenByDescending(version => version.VersionNumber).FirstOrDefaultAsync(cancellationToken);

    private static CustomQuoteRequest QuoteRequest(CustomDesign design, int quantity) => new()
    {
        Width = design.Width, Height = design.Height, Quantity = quantity, ShapeCode = design.ShapeCode, MaterialCode = design.MaterialCode,
        FinishCode = design.FinishCode, FontCode = design.FontCode, ColourCode = design.ColourCode, CustomText = design.CustomText, TextAlignment = design.TextAlignment
    };

    private static CartResponse ToCart(IEnumerable<CartItem> items)
    {
        var mapped = items.Select(ToItem).ToList();
        return new CartResponse { Items = mapped, Total = mapped.Sum(item => item.Subtotal), TotalItems = mapped.Sum(item => item.Quantity) };
    }

    private static CartItemResponse ToItem(CartItem item)
    {
        if (item.ItemType == CartItemType.CustomDesign && item.CustomDesign != null)
        {
            var design = item.CustomDesign; var options = design.BuilderConfigurationVersion?.Options ?? Array.Empty<CustomBuilderOption>();
            string? Label(BuilderOptionGroup group, string? code) => options.FirstOrDefault(option => option.Group == group && option.Code == code)?.Label;
            return new CartItemResponse
            {
                Id = item.Id, ItemType = "CustomDesign", CustomDesignId = design.Id, ProductName = design.Name, DesignName = design.Name,
                ProductPrice = item.UnitPriceSnapshot, Quantity = design.Quantity, Subtotal = item.UnitPriceSnapshot * design.Quantity,
                Width = design.Width, Height = design.Height, MeasurementUnit = design.BuilderConfigurationVersion?.MeasurementUnit,
                Shape = Label(BuilderOptionGroup.Shape, design.ShapeCode), Material = Label(BuilderOptionGroup.Material, design.MaterialCode),
                Finish = Label(BuilderOptionGroup.Finish, design.FinishCode), CustomText = design.CustomText, EditUrl = $"/custom/{design.Id}",
                IsOutOfStock = false, ProductStock = int.MaxValue, CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt
            };
        }
        var product = item.Product;
        return new CartItemResponse
        {
            Id = item.Id, ItemType = "Product", ProductId = item.ProductId, ProductName = product?.Name ?? "Unavailable product",
            ProductPrice = product?.Price ?? item.UnitPriceSnapshot, ProductImageUrl = product?.ImageUrl ?? string.Empty, Quantity = item.Quantity,
            Subtotal = (product?.Price ?? item.UnitPriceSnapshot) * item.Quantity, IsOutOfStock = product == null || product.Stock == 0,
            ProductStock = product?.Stock ?? 0, CreatedAt = item.CreatedAt, UpdatedAt = item.UpdatedAt
        };
    }
}
