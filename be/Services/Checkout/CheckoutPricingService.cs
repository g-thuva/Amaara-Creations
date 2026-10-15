using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using be.Data;
using be.DTOs.Checkout;
using be.DTOs.CustomBuilder;
using be.Models;
using be.Services.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace be.Services.Checkout;

public record PricedCheckout(CheckoutQuoteDto Quote, Address Address, List<CartItem> Cart, List<OrderItem> Items, Promotion? Promotion, ShippingOptionDto? Shipping);

public sealed class CheckoutPricingService(ApplicationDbContext db, ICustomStickerPricingService customPricing, PromotionService promotions, IPaymentGateway gateway, IOptions<CheckoutOptions> options, IWebHostEnvironment environment)
{
    public async Task<PricedCheckout> CalculateAsync(string userId, CheckoutQuoteRequest request, CancellationToken ct)
    {
        var settings = options.Value;
        ValidateSettings(settings);
        var address = await db.Addresses.SingleOrDefaultAsync(a => a.Id == request.AddressId && a.UserId == userId, ct)
            ?? throw new CommerceException("ADDRESS_INVALID", "Choose one of your saved addresses.", 404);
        if (new[] { address.RecipientName, address.AddressLine1, address.City, address.Country }.Any(string.IsNullOrWhiteSpace))
            throw new CommerceException("ADDRESS_INVALID", "Complete the recipient, street, city and country in your address book.");
        var cart = await db.CartItems.Include(i => i.Product).Include(i => i.CustomDesign)!.ThenInclude(d => d!.Assets)
            .Include(i => i.CustomDesign)!.ThenInclude(d => d!.BuilderConfigurationVersion)
            .Where(i => i.UserId == userId).OrderBy(i => i.Id).AsSplitQuery().ToListAsync(ct);
        if (cart.Count == 0) throw new CommerceException("CART_EMPTY", "Your cart is empty.");
        var configuration = cart.Any(i => i.ItemType == CartItemType.CustomDesign)
            ? await db.CustomBuilderConfigurationVersions.Include(c => c.Options).Where(c => c.Status == BuilderVersionStatus.Published).OrderByDescending(c => c.PublishedAt).ThenByDescending(c => c.VersionNumber).FirstOrDefaultAsync(ct) : null;
        var lines = new List<OrderItem>();
        foreach (var item in cart)
        {
            if (item.Quantity <= 0) throw new CommerceException("CART_INVALID", "A cart quantity is invalid.");
            var line = new OrderItem { SourceCartItemId = item.Id, SourceCartUpdatedAt = item.UpdatedAt, Quantity = item.Quantity };
            if (item.ItemType == CartItemType.CustomDesign)
            {
                var design = item.CustomDesign;
                if (design == null || design.UserId != userId || design.Status != CustomDesignStatus.InCart || design.Quantity != item.Quantity)
                    throw new CommerceException("CUSTOM_DESIGN_UNAVAILABLE", "A custom design is unavailable or already in a pending order.", 409);
                if (design.BuilderConfigurationVersion?.Currency != settings.Currency)
                    throw new CommerceException("PAYMENT_CURRENCY_MISMATCH", "A custom design was priced in another currency. Update it before checkout.", 409);
                if (configuration == null) throw new CommerceException("CUSTOM_BUILDER_UNAVAILABLE", "The custom builder is unavailable.", 503);
                if (configuration.Currency != settings.Currency) throw new CommerceException("PAYMENT_CURRENCY_MISMATCH", "Custom and standard prices must use the same store currency.", 409);
                if (configuration.ArtworkRequired && !design.Assets.Any(a => a.AssetType == "artwork")) throw new CommerceException("ARTWORK_REQUIRED", "Upload the required custom artwork before checkout.");
                CustomQuoteResponse quote;
                try { quote = customPricing.Quote(configuration, Request(design)); }
                catch (CustomBuilderValidationException) { throw new CommerceException("CUSTOM_DESIGN_INVALID", "A custom design needs to be updated in the builder."); }
                line.ItemType = OrderItemType.CustomDesign; line.CustomDesignId = design.Id; line.ProductNameSnapshot = design.Name;
                line.SkuSnapshot = $"CUSTOM-{design.Id}"; line.Price = line.UnitPrice = quote.UnitPrice; line.Subtotal = quote.Subtotal;
                line.CustomDesignSnapshotVersion = 1; line.ConfigurationSnapshotJson = Snapshot(design, configuration, quote);
            }
            else
            {
                var product = item.Product;
                if (product == null || !product.IsActive || product.Price < 0) throw new CommerceException("PRODUCT_UNAVAILABLE", "A product is no longer available.");
                if (product.Currency != settings.Currency) throw new CommerceException("PAYMENT_CURRENCY_MISMATCH", "A product is priced in another currency. Remove it before checkout.", 409);
                if (product.Stock < item.Quantity) throw new CommerceException("INSUFFICIENT_STOCK", $"There is insufficient stock for {product.Name}.", 409);
                line.ProductId = product.Id; line.ProductNameSnapshot = product.Name; line.SkuSnapshot = product.BaseSku; line.ImageSnapshot = product.ImageUrl;
                line.Price = line.UnitPrice = product.Price; line.Subtotal = Money.Round(product.Price * item.Quantity, settings.Currency);
            }
            lines.Add(line);
        }
        var subtotal = lines.Sum(i => i.Subtotal);
        var methods = await db.ShippingMethods.Include(m => m.ShippingZone).Where(m => m.IsActive).OrderBy(m => m.DisplayOrder).ThenBy(m => m.Id).ToListAsync(ct);
        var shippingOptions = methods.Where(m => ShippingService.Eligible(m, address, subtotal, settings.Currency, environment.IsDevelopment() && settings.AllowDevelopmentShipping)).Select(m => ShippingService.Option(m, subtotal)).ToList();
        var shipping = shippingOptions.SingleOrDefault(m => m.Id == request.ShippingMethodId);
        if (request.ShippingMethodId.HasValue && shipping == null) throw new CommerceException("SHIPPING_UNAVAILABLE", "This delivery method is unavailable for your address.");
        var (promotion, coupon) = await promotions.EvaluateAsync(request.CouponCode, userId, lines, subtotal, settings.Currency, ct);
        var discount = coupon?.Discount ?? 0;
        var totals = Totals(subtotal, discount, shipping?.Amount ?? 0, settings);
        var paymentAvailable = PaymentAvailable(settings.Currency);
        var itemDtos = lines.Select(i => new CheckoutItemDto(i.SourceCartItemId!.Value, i.ItemType.ToString(), i.ProductNameSnapshot, i.Quantity, i.UnitPrice, i.Subtotal, i.ConfigurationSnapshotJson)).ToList();
        var version = Fingerprint(new
        {
            userId, address.Id, address.RecipientName, address.AddressLine1, address.AddressLine2, address.City, address.Country, address.DistrictOrProvince, address.PostalCode, address.Phone,
            Items = itemDtos, CartVersions = cart.Select(i => new { i.Id, i.UpdatedAt, DesignVersion = i.CustomDesign?.UpdatedAt }), Shipping = shipping,
            Coupon = coupon, PromotionId = promotion?.Id, totals, settings.Currency, settings.TaxRate, settings.PricesIncludeTax, settings.TaxShipping
        });
        return new(new(itemDtos, subtotal, discount, shipping?.Amount ?? 0, totals.Tax, totals.Fee, totals.Total, settings.Currency, shippingOptions, coupon, version,
            DateTime.UtcNow.AddMinutes(5), paymentAvailable, paymentAvailable && shipping != null && (coupon == null || coupon.Applied) && totals.Total > 0, settings.PricesIncludeTax,
            paymentAvailable ? gateway.Provider : null, paymentAvailable ? gateway.Mode : null), address, cart, lines, promotion, shipping);
    }

    private bool PaymentAvailable(string currency)
    {
        try { gateway.EnsureAvailable(currency); return true; }
        catch (CommerceException) { return false; }
    }

    public static (decimal Tax, decimal Fee, decimal Total) Totals(decimal subtotal, decimal discount, decimal shipping, CheckoutOptions options)
    {
        ValidateSettings(options);
        var discounted = Math.Max(0, subtotal - Math.Clamp(discount, 0, subtotal));
        var taxable = discounted + (options.TaxShipping ? shipping : 0);
        var tax = Money.Round(options.PricesIncludeTax ? taxable - taxable / (1 + options.TaxRate / 100m) : taxable * options.TaxRate / 100m, options.Currency);
        var beforeFee = discounted + shipping + (options.PricesIncludeTax ? 0 : tax);
        var fee = Money.Round(options.PaymentFeeFixed + beforeFee * options.PaymentFeePercentage / 100m, options.Currency);
        return (tax, fee, Money.Round(beforeFee + fee, options.Currency));
    }
    public static void ValidateSettings(CheckoutOptions options)
    {
        if (options.TaxRate is < 0 or > 100 || options.PaymentFeeFixed < 0 || options.PaymentFeePercentage is < 0 or > 100 || options.InventoryReservationMinutes is < 30 or > 120 || options.ReconciliationSeconds < 10)
            throw new CommerceException("CHECKOUT_CONFIGURATION_INVALID", "Checkout configuration needs administrator attention.", 503);
        _ = Money.Exponent(options.Currency);
        if (Money.Exponent(options.Currency) > 2) throw new CommerceException("CURRENCY_UNSUPPORTED", "The store database currently supports currencies with up to two decimal places.", 503);
    }
    public static string Fingerprint(object value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value))));
    private static CustomQuoteRequest Request(CustomDesign d) => new() { Width = d.Width, Height = d.Height, Quantity = d.Quantity, ShapeCode = d.ShapeCode, MaterialCode = d.MaterialCode, FinishCode = d.FinishCode, FontCode = d.FontCode, ColourCode = d.ColourCode, CustomText = d.CustomText, TextAlignment = d.TextAlignment };
    public static string Snapshot(CustomDesign d, CustomBuilderConfigurationVersion c, CustomQuoteResponse q)
    {
        object Option(BuilderOptionGroup group, string? code) => new { Code = code, Label = c.Options.SingleOrDefault(o => o.Group == group && o.Code == code)?.Label };
        return JsonSerializer.Serialize(new
        {
            SnapshotVersion = 1, d.DesignSchemaVersion, DesignName = d.Name, ConfigurationVersionId = c.Id, ConfigurationVersion = c.VersionNumber, q.PricingVersion,
            c.MeasurementUnit, q.Width, q.Height, q.Quantity, Shape = Option(BuilderOptionGroup.Shape, d.ShapeCode), Material = Option(BuilderOptionGroup.Material, d.MaterialCode),
            Finish = Option(BuilderOptionGroup.Finish, d.FinishCode), Font = Option(BuilderOptionGroup.Font, d.FontCode), Colour = Option(BuilderOptionGroup.Colour, d.ColourCode),
            d.CustomText, d.TextAlignment, q.Currency, q.UnitPrice, q.Subtotal, c.ProofRequired, ArtworkAssetIds = d.Assets.Where(a => a.AssetType == "artwork").OrderBy(a => a.Id).Select(a => a.Id).ToArray()
        });
    }
}
