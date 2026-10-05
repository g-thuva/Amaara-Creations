using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Admin
{
    public class AdminProductQuery
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public string? Search { get; set; }

        public int? CategoryId { get; set; }

        public int? CollectionId { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsFeatured { get; set; }

        public string? SortBy { get; set; }

        public string? SortDirection { get; set; }
    }

    public class AdminProductRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(220)]
        public string? Slug { get; set; }

        [StringLength(300)]
        public string? ShortDescription { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal BasePrice { get; set; }

        [StringLength(64)]
        public string? BaseSku { get; set; }

        public int? CategoryId { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [Range(0, int.MaxValue)]
        public int Stock { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; }

        public List<int> CollectionIds { get; set; } = new();
    }

    public class CategoryRequest
    {
        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(140)]
        public string? Slug { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public int? ImageMediaId { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CollectionRequest
    {
        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [StringLength(140)]
        public string? Slug { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class CollectionProductsRequest
    {
        [Required]
        public List<int> ProductIds { get; set; } = new();
    }

    public class ProductVariantRequest
    {
        [Required]
        [StringLength(64)]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal? PriceOverride { get; set; }

        [Range(0, int.MaxValue)]
        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        public string? RowVersion { get; set; }
    }

    public class StockAdjustmentRequest
    {
        [Range(-100000, 100000)]
        public int QuantityDelta { get; set; }

        [Required]
        [StringLength(300)]
        public string Reason { get; set; } = string.Empty;
    }

    public class ProductMediaRequest
    {
        public int? ExistingMediaId { get; set; }

        [StringLength(200)]
        public string? AltText { get; set; }

        public int SortOrder { get; set; }

        public bool IsPrimary { get; set; }
    }

    public class ProductMediaUpdateRequest
    {
        [StringLength(200)]
        public string? AltText { get; set; }

        public int? SortOrder { get; set; }

        public bool? IsPrimary { get; set; }
    }

    public class CmsPageRequest
    {
        [Required]
        [StringLength(160)]
        public string Title { get; set; } = string.Empty;

        [StringLength(180)]
        public string? Slug { get; set; }

        [StringLength(300)]
        public string? Summary { get; set; }

        public bool IsPublished { get; set; }

        public List<CmsSectionRequest> Sections { get; set; } = new();
    }

    public class CmsSectionRequest
    {
        [Required]
        [StringLength(120)]
        public string SectionKey { get; set; } = string.Empty;

        [Required]
        [StringLength(80)]
        public string ContentType { get; set; } = "text";

        [Required]
        [StringLength(4000)]
        public string Content { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class SiteSettingRequest
    {
        [Required]
        [StringLength(120)]
        public string Key { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string Value { get; set; } = string.Empty;

        [StringLength(80)]
        public string ValueType { get; set; } = "text";

        public bool IsPublic { get; set; }

        [StringLength(300)]
        public string? Description { get; set; }
    }
}
