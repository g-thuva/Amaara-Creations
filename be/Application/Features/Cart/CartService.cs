using be.Application.Features.Checkout;
using be.Application.Features.CustomDesigns;
using be.Domain.Cart;
using be.Domain.CustomDesigns;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;
using be.Application.Common;
using be.Domain.Common;

namespace be.Application.Features.Cart;
public class CartService
{
    private readonly IApplicationDbContext _context;
    private readonly CustomStickerPricingService _pricing;
    private readonly IOptions<CheckoutOptions> _checkoutOptions;
    public CartService(
        IApplicationDbContext context,
        CustomStickerPricingService pricing,
        IOptions<CheckoutOptions> checkoutOptions,
        IRequestActor actor)
    {
        _context = context;
        _pricing = pricing;
        _checkoutOptions = checkoutOptions;
        _actor = actor;
    }

    public async Task<CartResponse> GetCart(CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null)
            throw new CommerceException("CART_UNAUTHORIZED", "Authentication is required.", 401);
        var items = await CartQuery().Where(item => item.UserId == userId).AsNoTracking().OrderBy(item => item.CreatedAt).ToListAsync(cancellationToken);
        return ToCart(items);
    }

    public async Task<CartItemResponse> AddToCart(AddToCartRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null)
            throw new CommerceException("CART_UNAUTHORIZED", "Authentication is required.", 401);
        await using var transaction = await _context.BeginCommerceTransactionAsync(cancellationToken);
        var product = await _context.Products.FirstOrDefaultAsync(item => item.Id == request.ProductId && item.IsActive, cancellationToken);
        if (product == null)
            throw new CommerceException("CART_NOT_FOUND", "Product not found", 404);
        if (product.Currency != _checkoutOptions.Value.Currency)
            throw new CommerceException("CART_CONFLICT", "This product is priced in another currency and cannot be added to this checkout.", 409);
        var variant = request.ProductVariantId.HasValue ? await _context.ProductVariants.SingleOrDefaultAsync(item => item.Id == request.ProductVariantId && item.ProductId == product.Id && item.IsActive, cancellationToken) : null;
        if (request.ProductVariantId.HasValue && variant == null) throw new CommerceException("VARIANT_UNAVAILABLE", "Choose an available option for this product.", 404);
        var stock = variant?.StockQuantity ?? product.Stock;
        var price = variant?.PriceOverride ?? product.Price;
        if (price < 0) throw new CommerceException("PRODUCT_UNAVAILABLE", "This product is unavailable.", 409);
        var cart = await CartQuery().Where(item => item.UserId == userId).AsNoTracking().ToListAsync(cancellationToken);
        if (cart.Any(item => ItemCurrency(item) != product.Currency))
            throw new CommerceException("CART_CONFLICT", "Remove items priced in another currency before adding this product.", 409);
        var existing = await _context.CartItems.FirstOrDefaultAsync(item => item.UserId == userId && item.ProductId == request.ProductId && item.ProductVariantId == request.ProductVariantId, cancellationToken);
        var quantity = (long)(existing?.Quantity ?? 0) + request.Quantity;
        if (request.Quantity < 1 || quantity > stock || quantity > int.MaxValue)
            throw new RequestValidationException(new Dictionary<string, string[]> { ["quantity"] = [$"Only {stock} items are available."] });
        var cartItem = existing ?? new CartItem
        {
            UserId = userId,
            ProductId = product.Id,
            ProductVariantId = variant?.Id,
            ItemType = CartItemType.Product,
            CreatedAt = DateTime.UtcNow
        };
        cartItem.Quantity = (int)quantity;
        cartItem.UnitPriceSnapshot = price;
        cartItem.UpdatedAt = DateTime.UtcNow;
        if (existing == null)
            _context.CartItems.Add(cartItem);
        await _context.SaveChangesAsync(cancellationToken);
        cartItem.Product = product;
        cartItem.ProductVariant = variant;
        if (transaction != null) await transaction.CommitAsync(cancellationToken);
        return ToItem(cartItem);
    }

    public async Task<CartItemResponse> UpdateCartItem(int itemId, UpdateCartItemRequest request, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null)
            throw new CommerceException("CART_UNAUTHORIZED", "Authentication is required.", 401);
        var item = await CartQuery().FirstOrDefaultAsync(value => value.Id == itemId && value.UserId == userId, cancellationToken);
        if (item == null)
            throw new CommerceException("CART_NOT_FOUND", "Resource not found.", 404);
        if (ItemCurrency(item) != _checkoutOptions.Value.Currency)
            throw new CommerceException("CART_CONFLICT", "This cart item is priced in another currency. Remove it before checkout.", 409);
        if (item.ItemType == CartItemType.Product)
        {
            var stock = item.ProductVariant?.StockQuantity ?? item.Product?.Stock ?? 0;
            if (item.Product is not { IsActive: true } || (item.ProductVariantId.HasValue && item.ProductVariant is not { IsActive: true }))
                throw new CommerceException("PRODUCT_UNAVAILABLE", "This product or option is no longer available.", 409);
            if (stock < request.Quantity)
                throw new RequestValidationException(new Dictionary<string, string[]> { ["quantity"] = [$"Only {stock} items are available."] });
            item.Quantity = request.Quantity;
            item.UnitPriceSnapshot = item.ProductVariant?.PriceOverride ?? item.Product.Price;
        }
        else
        {
            if (item.CustomDesign == null)
                throw new CommerceException("CART_CONFLICT", "Custom design is unavailable", 409);
            if (item.CustomDesign.Status == CustomDesignStatus.Ordered)
                throw new CommerceException("CART_CONFLICT", "This design is reserved for an unpaid order. Resolve its payment before editing.", 409);
            var configuration = await ActiveConfiguration(cancellationToken);
            if (configuration == null)
                throw new CommerceException("CART_REQUEST_INVALID", "Custom builder is not configured", 503);
            if (configuration.Currency != _checkoutOptions.Value.Currency)
                throw new CommerceException("CART_CONFLICT", "The custom builder is priced in another currency.", 409);
            var requestQuote = QuoteRequest(item.CustomDesign, request.Quantity);
            try
            {
                var quote = _pricing.Quote(configuration, requestQuote);
                item.Quantity = quote.Quantity;
                item.UnitPriceSnapshot = quote.UnitPrice;
                item.BuilderConfigurationVersionId = configuration.Id;
                item.CustomDesign.Quantity = quote.Quantity;
                item.CustomDesign.UnitPrice = quote.UnitPrice;
                item.CustomDesign.CalculatedPrice = quote.Subtotal;
                item.CustomDesign.PricingRuleVersion = quote.PricingVersion;
                item.CustomDesign.BuilderConfigurationVersionId = configuration.Id;
                item.CustomDesign.QuotedAt = quote.QuotedAt;
                item.CustomDesign.UpdatedAt = DateTime.UtcNow;
            }
            catch (CustomBuilderValidationException exception)
            {
                throw new RequestValidationException(new Dictionary<string, string[]> { ["quantity"] = exception.Errors.ToArray() });
            }
        }

        item.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(cancellationToken);
        return ToItem(item);
    }

    public async Task RemoveCartItem(int itemId, CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null)
            throw new CommerceException("CART_UNAUTHORIZED", "Authentication is required.", 401);
        var item = await _context.CartItems.Include(value => value.CustomDesign)
            .FirstOrDefaultAsync(value => value.Id == itemId && value.UserId == userId, cancellationToken);
        if (item == null)
            throw new CommerceException("CART_NOT_FOUND", "Resource not found.", 404);
        if (item.CustomDesign != null && item.CustomDesign.Status == CustomDesignStatus.InCart)
            item.CustomDesign.Status = CustomDesignStatus.Ready;
        _context.CartItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);
        return;
    }

    public async Task ClearCart(CancellationToken cancellationToken)
    {
        var userId = UserId();
        if (userId == null)
            throw new CommerceException("CART_UNAUTHORIZED", "Authentication is required.", 401);
        var items = await _context.CartItems.Include(item => item.CustomDesign).Where(item => item.UserId == userId).ToListAsync(cancellationToken);
        foreach (var item in items.Where(item => item.CustomDesign?.Status == CustomDesignStatus.InCart))
            item.CustomDesign!.Status = CustomDesignStatus.Ready;
        _context.CartItems.RemoveRange(items);
        await _context.SaveChangesAsync(cancellationToken);
        return;
    }

    private string? UserId() => _actor.User.FindFirstValue(ClaimTypes.NameIdentifier);
    private IQueryable<CartItem> CartQuery() => _context.CartItems.Include(item => item.Product).Include(item => item.ProductVariant).Include(item => item.CustomDesign)!.ThenInclude(design => design!.Assets).Include(item => item.CustomDesign)!.ThenInclude(design => design!.BuilderConfigurationVersion)!.ThenInclude(version => version!.Options).AsSplitQuery();
    private async Task<CustomBuilderConfigurationVersion?> ActiveConfiguration(CancellationToken cancellationToken) => await _context.CustomBuilderConfigurationVersions.Include(version => version.Options)
        .Where(version => version.Status == BuilderVersionStatus.Published && version.Currency == _checkoutOptions.Value.Currency)
        .OrderByDescending(version => version.PublishedAt)
        .ThenByDescending(version => version.VersionNumber)
        .FirstOrDefaultAsync(cancellationToken);
    private static CustomQuoteRequest QuoteRequest(CustomDesign design, int quantity) => new()
    {
        Width = design.Width,
        Height = design.Height,
        Quantity = quantity,
        ShapeCode = design.ShapeCode,
        MaterialCode = design.MaterialCode,
        FinishCode = design.FinishCode,
        FontCode = design.FontCode,
        ColourCode = design.ColourCode,
        CustomText = design.CustomText,
        TextAlignment = design.TextAlignment
    };
    private CartResponse ToCart(IEnumerable<CartItem> items)
    {
        var mapped = items.Select(ToItem).ToList();
        var currencies = mapped.Select(item => item.Currency).Distinct(StringComparer.Ordinal).ToArray();
        var currency = currencies.Length == 1 ? currencies[0] : null;
        return new CartResponse
        {
            Items = mapped,
            Currency = currency,
            HasMixedCurrencies = currencies.Length > 1,
            CanCheckout = currency != null && currency == _checkoutOptions.Value.Currency,
            Total = currency == null ? 0 : mapped.Sum(item => item.Subtotal),
            TotalItems = mapped.Sum(item => item.Quantity)
        };
    }

    private static string ItemCurrency(CartItem item) => item.ItemType == CartItemType.CustomDesign ? item.CustomDesign?.BuilderConfigurationVersion?.Currency ?? Money.DefaultCurrency : item.Product?.Currency ?? Money.DefaultCurrency;
    private static CartItemResponse ToItem(CartItem item)
    {
        if (item.ItemType == CartItemType.CustomDesign && item.CustomDesign != null)
        {
            var design = item.CustomDesign;
            var options = design.BuilderConfigurationVersion?.Options ?? Array.Empty<CustomBuilderOption>();
            string? Label(BuilderOptionGroup group, string? code) => options.FirstOrDefault(option => option.Group == group && option.Code == code)?.Label;
            return new CartItemResponse
            {
                Id = item.Id,
                ItemType = "CustomDesign",
                CustomDesignId = design.Id,
                ProductName = design.Name,
                DesignName = design.Name,
                ProductPrice = item.UnitPriceSnapshot,
                Quantity = design.Quantity,
                Subtotal = item.UnitPriceSnapshot * design.Quantity,
                Currency = ItemCurrency(item),
                Width = design.Width,
                Height = design.Height,
                MeasurementUnit = design.BuilderConfigurationVersion?.MeasurementUnit,
                Shape = Label(BuilderOptionGroup.Shape, design.ShapeCode),
                Material = Label(BuilderOptionGroup.Material, design.MaterialCode),
                Finish = Label(BuilderOptionGroup.Finish, design.FinishCode),
                CustomText = design.CustomText,
                EditUrl = $"/custom/{design.Id}",
                ArtworkDownloadUrl = design.Assets.Where(asset => asset.AssetType == "artwork").OrderByDescending(asset => asset.Id).Select(asset => $"/api/v1/custom-designs/{design.Id}/assets/{asset.Id}").FirstOrDefault(),
                IsOutOfStock = false,
                ProductStock = int.MaxValue,
                CreatedAt = item.CreatedAt,
                UpdatedAt = item.UpdatedAt
            };
        }

        var product = item.Product;
        var variant = item.ProductVariant;
        var available = product is { IsActive: true } && (!item.ProductVariantId.HasValue || variant is { IsActive: true });
        var unitPrice = variant?.PriceOverride ?? product?.Price ?? item.UnitPriceSnapshot;
        var stock = variant?.StockQuantity ?? product?.Stock ?? 0;
        return new CartItemResponse
        {
            Id = item.Id,
            ItemType = "Product",
            ProductId = item.ProductId,
            ProductVariantId = item.ProductVariantId,
            VariantName = variant?.Name,
            ProductName = product == null ? "Unavailable product" : variant == null ? product.Name : $"{product.Name} — {variant.Name}",
            ProductPrice = unitPrice,
            ProductImageUrl = product?.ImageUrl ?? string.Empty,
            Quantity = item.Quantity,
            Currency = ItemCurrency(item),
            Subtotal = unitPrice * item.Quantity,
            IsOutOfStock = !available || stock <= 0,
            ProductStock = available ? stock : 0,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }

    private readonly IRequestActor _actor;
}
