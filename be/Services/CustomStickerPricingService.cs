using be.DTOs.CustomBuilder;
using be.Models;

namespace be.Services;

public sealed class CustomBuilderValidationException : Exception
{
    public CustomBuilderValidationException(IEnumerable<string> errors)
        : base("The custom sticker configuration is invalid.")
    {
        Errors = errors.ToArray();
    }

    public IReadOnlyList<string> Errors { get; }
}

public interface ICustomStickerPricingService
{
    CustomQuoteResponse Quote(CustomBuilderConfigurationVersion configuration, CustomQuoteRequest request);
    IReadOnlyList<string> ValidateConfiguration(CustomBuilderConfigurationVersion configuration);
}

public class CustomStickerPricingService : ICustomStickerPricingService
{
    public CustomQuoteResponse Quote(CustomBuilderConfigurationVersion configuration, CustomQuoteRequest request)
    {
        var errors = ValidateConfiguration(configuration).ToList();
        ValidateRange(request.Width, configuration.MinimumWidth, configuration.MaximumWidth, configuration.WidthStep, "Width", errors);
        ValidateRange(request.Height, configuration.MinimumHeight, configuration.MaximumHeight, configuration.HeightStep, "Height", errors);
        ValidateRange(request.Quantity, configuration.MinimumQuantity, configuration.MaximumQuantity, configuration.QuantityStep, "Quantity", errors);

        var selected = new List<CustomBuilderOption>();
        SelectOption(configuration, BuilderOptionGroup.Shape, request.ShapeCode, selected, errors);
        SelectOption(configuration, BuilderOptionGroup.Material, request.MaterialCode, selected, errors);
        SelectOption(configuration, BuilderOptionGroup.Finish, request.FinishCode, selected, errors);
        SelectOption(configuration, BuilderOptionGroup.Font, request.FontCode, selected, errors);
        SelectOption(configuration, BuilderOptionGroup.Colour, request.ColourCode, selected, errors);

        if (errors.Count > 0)
        {
            throw new CustomBuilderValidationException(errors);
        }

        var area = request.Width * request.Height;
        var baseAmount = area * configuration.PricePerSquareUnit;
        var adjustment = selected.Sum(option => option.UnitPriceAdjustment);
        var rawUnitPrice = baseAmount + adjustment;
        var minimumUnitPrice = request.Quantity > 0 ? configuration.MinimumLinePrice / request.Quantity : 0m;
        var unitPrice = decimal.Round(Math.Max(rawUnitPrice, minimumUnitPrice), 2, MidpointRounding.AwayFromZero);
        var subtotal = decimal.Round(unitPrice * request.Quantity, 2, MidpointRounding.AwayFromZero);
        var now = DateTime.UtcNow;

        var breakdown = new List<QuoteBreakdownItemDto>
        {
            new() { Label = "Base sticker", AmountPerUnit = decimal.Round(baseAmount, 2, MidpointRounding.AwayFromZero) }
        };
        breakdown.AddRange(selected.Where(option => option.UnitPriceAdjustment != 0m).Select(option => new QuoteBreakdownItemDto
        {
            Label = option.Label,
            AmountPerUnit = option.UnitPriceAdjustment
        }));

        return new CustomQuoteResponse
        {
            ConfigurationVersionId = configuration.Id,
            ConfigurationVersion = configuration.VersionNumber,
            PricingVersion = $"config-v{configuration.VersionNumber}",
            Currency = configuration.Currency,
            Width = request.Width,
            Height = request.Height,
            Quantity = request.Quantity,
            UnitPrice = unitPrice,
            Subtotal = subtotal,
            QuotedAt = now,
            Breakdown = breakdown
        };
    }

    public IReadOnlyList<string> ValidateConfiguration(CustomBuilderConfigurationVersion configuration)
    {
        var errors = new List<string>();
        if (configuration.MinimumWidth <= 0 || configuration.MaximumWidth < configuration.MinimumWidth) errors.Add("Width limits are invalid.");
        if (configuration.MinimumHeight <= 0 || configuration.MaximumHeight < configuration.MinimumHeight) errors.Add("Height limits are invalid.");
        if (configuration.WidthStep <= 0 || configuration.HeightStep <= 0) errors.Add("Dimension steps must be greater than zero.");
        if (configuration.MinimumQuantity <= 0 || configuration.MaximumQuantity < configuration.MinimumQuantity || configuration.QuantityStep <= 0) errors.Add("Quantity limits are invalid.");
        if (configuration.PricePerSquareUnit < 0 || configuration.MinimumLinePrice < 0) errors.Add("Pricing values cannot be negative.");
        if (configuration.Options.Any(option => option.UnitPriceAdjustment < 0)) errors.Add("Option price adjustments cannot be negative.");
        if (string.IsNullOrWhiteSpace(configuration.Currency) || configuration.Currency.Length != 3) errors.Add("Currency must be a three-letter code.");
        return errors;
    }

    private static void SelectOption(CustomBuilderConfigurationVersion configuration, BuilderOptionGroup group, string? code, ICollection<CustomBuilderOption> selected, ICollection<string> errors)
    {
        var available = configuration.Options.Where(option => option.Group == group && option.IsActive).ToList();
        if (available.Count == 0) return;
        var option = available.FirstOrDefault(value => string.Equals(value.Code, code, StringComparison.OrdinalIgnoreCase));
        if (option == null) errors.Add($"Select a valid {group.ToString().ToLowerInvariant()} option.");
        else selected.Add(option);
    }

    private static void ValidateRange(decimal value, decimal minimum, decimal maximum, decimal step, string label, ICollection<string> errors)
    {
        if (value < minimum || value > maximum || step <= 0 || decimal.Remainder(value - minimum, step) != 0)
        {
            errors.Add($"{label} must be between {minimum} and {maximum} in increments of {step}.");
        }
    }

    private static void ValidateRange(int value, int minimum, int maximum, int step, string label, ICollection<string> errors)
    {
        if (value < minimum || value > maximum || step <= 0 || (value - minimum) % step != 0)
        {
            errors.Add($"{label} must be between {minimum} and {maximum} in increments of {step}.");
        }
    }
}
