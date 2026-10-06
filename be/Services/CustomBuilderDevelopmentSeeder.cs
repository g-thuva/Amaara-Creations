using be.Data;
using be.Models;
using Microsoft.EntityFrameworkCore;

namespace be.Services;

public static class CustomBuilderDevelopmentSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        if (await context.CustomBuilderConfigurationVersions.AnyAsync()) return;

        var published = CreateVersion(1, BuilderVersionStatus.Published);
        published.PublishedAt = DateTime.UtcNow;
        published.InternalNotes = "DEVELOPMENT INITIAL CONFIGURATION. Commercial pricing and production constraints require business-owner verification.";
        var draft = CreateVersion(2, BuilderVersionStatus.Draft);
        draft.InternalNotes = published.InternalNotes;
        context.CustomBuilderConfigurationVersions.AddRange(published, draft);
        await context.SaveChangesAsync();
    }

    private static CustomBuilderConfigurationVersion CreateVersion(int number, BuilderVersionStatus status)
    {
        var version = new CustomBuilderConfigurationVersion
        {
            VersionNumber = number,
            Status = status,
            MeasurementUnit = "cm",
            MinimumWidth = 2m,
            MaximumWidth = 30m,
            WidthStep = 1m,
            MinimumHeight = 2m,
            MaximumHeight = 30m,
            HeightStep = 1m,
            MinimumQuantity = 1,
            MaximumQuantity = 100,
            QuantityStep = 1,
            PricePerSquareUnit = 1m,
            MinimumLinePrice = 0m,
            Currency = "LKR",
            ArtworkRequired = false,
            ProofRequired = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        version.Options = new List<CustomBuilderOption>
        {
            Option(version, BuilderOptionGroup.Shape, "rectangle", "Rectangle", 0),
            Option(version, BuilderOptionGroup.Shape, "round", "Round", 1),
            Option(version, BuilderOptionGroup.Material, "standard", "Standard sticker material", 0),
            Option(version, BuilderOptionGroup.Finish, "standard", "Standard finish", 0),
            Option(version, BuilderOptionGroup.Font, "inter", "Clean sans serif", 0),
            Option(version, BuilderOptionGroup.Font, "poppins", "Bold display", 1),
            Option(version, BuilderOptionGroup.Colour, "navy", "Navy", 0),
            Option(version, BuilderOptionGroup.Colour, "white", "White", 1)
        };
        return version;
    }

    private static CustomBuilderOption Option(CustomBuilderConfigurationVersion version, BuilderOptionGroup group, string code, string label, int order)
    {
        return new CustomBuilderOption { ConfigurationVersion = version, Group = group, Code = code, Label = label, SortOrder = order, IsActive = true, UnitPriceAdjustment = 0m };
    }
}
