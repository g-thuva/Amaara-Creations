using be.DTOs.CustomBuilder;
using be.Models;

namespace be.Services;

public static class CustomBuilderMappings
{
    public static CustomBuilderConfigurationDto ToDto(this CustomBuilderConfigurationVersion version, bool includePrivatePricing)
    {
        return new CustomBuilderConfigurationDto
        {
            Id = version.Id,
            VersionNumber = version.VersionNumber,
            Status = version.Status.ToString(),
            MeasurementUnit = version.MeasurementUnit,
            MinimumWidth = version.MinimumWidth,
            MaximumWidth = version.MaximumWidth,
            WidthStep = version.WidthStep,
            MinimumHeight = version.MinimumHeight,
            MaximumHeight = version.MaximumHeight,
            HeightStep = version.HeightStep,
            MinimumQuantity = version.MinimumQuantity,
            MaximumQuantity = version.MaximumQuantity,
            QuantityStep = version.QuantityStep,
            Currency = version.Currency,
            ArtworkRequired = version.ArtworkRequired,
            ProofRequired = version.ProofRequired,
            PricePerSquareUnit = includePrivatePricing ? version.PricePerSquareUnit : null,
            MinimumLinePrice = includePrivatePricing ? version.MinimumLinePrice : null,
            InternalNotes = includePrivatePricing ? version.InternalNotes : null,
            PublishedAt = version.PublishedAt,
            RowVersion = includePrivatePricing ? Convert.ToBase64String(version.RowVersion) : null,
            Options = version.Options.OrderBy(option => option.Group).ThenBy(option => option.SortOrder).Select(option => new CustomBuilderOptionDto
            {
                Id = option.Id,
                Group = option.Group.ToString(),
                Code = option.Code,
                Label = option.Label,
                UnitPriceAdjustment = includePrivatePricing ? option.UnitPriceAdjustment : null,
                IsActive = option.IsActive,
                SortOrder = option.SortOrder
            }).ToList()
        };
    }

    public static CustomDesignDto ToDto(this CustomDesign design, bool includeProofs = true)
    {
        var options = design.BuilderConfigurationVersion?.Options ?? Array.Empty<CustomBuilderOption>();
        string? Label(BuilderOptionGroup group, string? code) => options.FirstOrDefault(option => option.Group == group && option.Code == code)?.Label;
        return new CustomDesignDto
        {
            Id = design.Id,
            Name = design.Name,
            DesignSchemaVersion = design.DesignSchemaVersion,
            BuilderConfigurationVersionId = design.BuilderConfigurationVersionId,
            BuilderConfigurationVersion = design.BuilderConfigurationVersion?.VersionNumber,
            MeasurementUnit = design.BuilderConfigurationVersion?.MeasurementUnit,
            Status = design.Status.ToString(),
            Width = design.Width,
            Height = design.Height,
            Quantity = design.Quantity,
            ShapeCode = design.ShapeCode,
            ShapeLabel = Label(BuilderOptionGroup.Shape, design.ShapeCode),
            MaterialCode = design.MaterialCode,
            MaterialLabel = Label(BuilderOptionGroup.Material, design.MaterialCode),
            FinishCode = design.FinishCode,
            FinishLabel = Label(BuilderOptionGroup.Finish, design.FinishCode),
            FontCode = design.FontCode,
            FontLabel = Label(BuilderOptionGroup.Font, design.FontCode),
            ColourCode = design.ColourCode,
            ColourLabel = Label(BuilderOptionGroup.Colour, design.ColourCode),
            CustomText = design.CustomText,
            TextAlignment = design.TextAlignment,
            UnitPrice = design.UnitPrice,
            Subtotal = design.CalculatedPrice,
            PricingVersion = design.PricingRuleVersion,
            QuotedAt = design.QuotedAt,
            ProofStatus = design.ProofStatus.ToString(),
            ProductionStatus = design.ProductionStatus.ToString(),
            EditorStateJson = design.DesignJson,
            CreatedAt = design.CreatedAt,
            UpdatedAt = design.UpdatedAt,
            RowVersion = Convert.ToBase64String(design.RowVersion),
            Assets = design.Assets.Where(asset => asset.AssetType != "proof").OrderBy(asset => asset.CreatedAt).Select(asset => new CustomDesignAssetDto
            {
                Id = asset.Id,
                AssetType = asset.AssetType,
                OriginalFileName = asset.OriginalFileName,
                ContentType = asset.ContentType,
                SizeBytes = asset.SizeBytes,
                Width = asset.Width,
                Height = asset.Height,
                CreatedAt = asset.CreatedAt,
                DownloadUrl = $"/api/v1/custom-designs/{design.Id}/assets/{asset.Id}"
            }).ToList(),
            ProofRevisions = includeProofs ? design.ProofRevisions.OrderByDescending(proof => proof.RevisionNumber).Select(proof => new ProofRevisionDto
            {
                Id = proof.Id,
                RevisionNumber = proof.RevisionNumber,
                Status = proof.Status.ToString(),
                CustomerVisibleMessage = proof.CustomerVisibleMessage,
                CustomerResponse = proof.CustomerResponse,
                CustomerResponseAt = proof.CustomerResponseAt,
                CreatedAt = proof.CreatedAt,
                AssetId = proof.AssetId,
                DownloadUrl = $"/api/v1/custom-designs/{design.Id}/proofs/{proof.RevisionNumber}/file"
            }).ToList() : new()
        };
    }
}
