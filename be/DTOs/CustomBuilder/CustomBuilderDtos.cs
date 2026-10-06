using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace be.DTOs.CustomBuilder;

public class CustomBuilderOptionDto
{
    public int? Id { get; set; }
    [Required, StringLength(40)] public string Group { get; set; } = string.Empty;
    [Required, StringLength(80)] public string Code { get; set; } = string.Empty;
    [Required, StringLength(120)] public string Label { get; set; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? UnitPriceAdjustment { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}

public class CustomBuilderConfigurationDto
{
    public int Id { get; set; }
    public int VersionNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string MeasurementUnit { get; set; } = "cm";
    public decimal MinimumWidth { get; set; }
    public decimal MaximumWidth { get; set; }
    public decimal WidthStep { get; set; }
    public decimal MinimumHeight { get; set; }
    public decimal MaximumHeight { get; set; }
    public decimal HeightStep { get; set; }
    public int MinimumQuantity { get; set; }
    public int MaximumQuantity { get; set; }
    public int QuantityStep { get; set; }
    public string Currency { get; set; } = "LKR";
    public bool ArtworkRequired { get; set; }
    public bool ProofRequired { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? PricePerSquareUnit { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public decimal? MinimumLinePrice { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? InternalNotes { get; set; }
    public DateTime? PublishedAt { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? RowVersion { get; set; }
    public List<CustomBuilderOptionDto> Options { get; set; } = new();
}

public class UpdateBuilderConfigurationRequest
{
    [Required, StringLength(20)] public string MeasurementUnit { get; set; } = "cm";
    public decimal MinimumWidth { get; set; }
    public decimal MaximumWidth { get; set; }
    public decimal WidthStep { get; set; }
    public decimal MinimumHeight { get; set; }
    public decimal MaximumHeight { get; set; }
    public decimal HeightStep { get; set; }
    public int MinimumQuantity { get; set; }
    public int MaximumQuantity { get; set; }
    public int QuantityStep { get; set; }
    public decimal PricePerSquareUnit { get; set; }
    public decimal MinimumLinePrice { get; set; }
    [Required, StringLength(3)] public string Currency { get; set; } = "LKR";
    public bool ArtworkRequired { get; set; }
    public bool ProofRequired { get; set; }
    [StringLength(500)] public string? InternalNotes { get; set; }
    public string? RowVersion { get; set; }
    public List<CustomBuilderOptionDto> Options { get; set; } = new();
}

public class CustomQuoteRequest
{
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public int Quantity { get; set; }
    [StringLength(80)] public string? ShapeCode { get; set; }
    [StringLength(80)] public string? MaterialCode { get; set; }
    [StringLength(80)] public string? FinishCode { get; set; }
    [StringLength(80)] public string? FontCode { get; set; }
    [StringLength(80)] public string? ColourCode { get; set; }
    [StringLength(160)] public string? CustomText { get; set; }
    [RegularExpression("^(left|center|right)$")] public string TextAlignment { get; set; } = "center";
}

public class QuoteBreakdownItemDto
{
    public string Label { get; set; } = string.Empty;
    public decimal AmountPerUnit { get; set; }
}

public class CustomQuoteResponse
{
    public int ConfigurationVersionId { get; set; }
    public int ConfigurationVersion { get; set; }
    public string PricingVersion { get; set; } = string.Empty;
    public string Currency { get; set; } = "LKR";
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
    public DateTime QuotedAt { get; set; }
    public List<QuoteBreakdownItemDto> Breakdown { get; set; } = new();
}

public class SaveCustomDesignRequest : CustomQuoteRequest
{
    [Required, StringLength(160)] public string Name { get; set; } = "Untitled custom sticker";
    [StringLength(8000)] public string? EditorStateJson { get; set; }
    public string? RowVersion { get; set; }
}

public class CustomDesignAssetDto
{
    public int Id { get; set; }
    public string AssetType { get; set; } = string.Empty;
    public string? OriginalFileName { get; set; }
    public string? ContentType { get; set; }
    public long? SizeBytes { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public DateTime CreatedAt { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public class ProofRevisionDto
{
    public int Id { get; set; }
    public int RevisionNumber { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? CustomerVisibleMessage { get; set; }
    public string? CustomerResponse { get; set; }
    public DateTime? CustomerResponseAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AssetId { get; set; }
    public string DownloadUrl { get; set; } = string.Empty;
}

public class CustomDesignDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DesignSchemaVersion { get; set; }
    public int? BuilderConfigurationVersionId { get; set; }
    public int? BuilderConfigurationVersion { get; set; }
    public string? MeasurementUnit { get; set; }
    public string Status { get; set; } = string.Empty;
    public decimal Width { get; set; }
    public decimal Height { get; set; }
    public int Quantity { get; set; }
    public string? ShapeCode { get; set; }
    public string? ShapeLabel { get; set; }
    public string? MaterialCode { get; set; }
    public string? MaterialLabel { get; set; }
    public string? FinishCode { get; set; }
    public string? FinishLabel { get; set; }
    public string? FontCode { get; set; }
    public string? FontLabel { get; set; }
    public string? ColourCode { get; set; }
    public string? ColourLabel { get; set; }
    public string? CustomText { get; set; }
    public string TextAlignment { get; set; } = "center";
    public decimal? UnitPrice { get; set; }
    public decimal? Subtotal { get; set; }
    public string? PricingVersion { get; set; }
    public DateTime? QuotedAt { get; set; }
    public string ProofStatus { get; set; } = string.Empty;
    public string ProductionStatus { get; set; } = string.Empty;
    public string? EditorStateJson { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<CustomDesignAssetDto> Assets { get; set; } = new();
    public List<ProofRevisionDto> ProofRevisions { get; set; } = new();
}

public class ProofResponseRequest
{
    public int RevisionNumber { get; set; }
    [StringLength(1000)] public string? Comment { get; set; }
}

public class UpdateProductionStatusRequest
{
    [Required] public string Status { get; set; } = string.Empty;
}
