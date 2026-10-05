namespace be.DTOs.Admin
{
    public class AdminProductResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? ShortDescription { get; set; }
        public decimal BasePrice { get; set; }
        public decimal Price { get; set; }
        public string? BaseSku { get; set; }
        public string Category { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int Stock { get; set; }
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int VariantCount { get; set; }
        public int MediaCount { get; set; }
        public List<int> CollectionIds { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CategoryResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public int? ImageMediaId { get; set; }
        public string? ImageUrl { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CollectionResponse
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
        public int ProductCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ProductVariantResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal? PriceOverride { get; set; }
        public int StockQuantity { get; set; }
        public bool IsActive { get; set; }
        public string RowVersion { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class ProductMediaResponse
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string StorageKey { get; set; } = string.Empty;
        public string? Url { get; set; }
        public string MediaType { get; set; } = "image";
        public string? AltText { get; set; }
        public string? OriginalFileName { get; set; }
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public int? Width { get; set; }
        public int? Height { get; set; }
        public string StorageProvider { get; set; } = "local";
        public int SortOrder { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CmsPageResponse
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Summary { get; set; }
        public bool IsPublished { get; set; }
        public DateTime? PublishedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<CmsSectionResponse> Sections { get; set; } = new();
    }

    public class CmsSectionResponse
    {
        public int Id { get; set; }
        public int CmsPageId { get; set; }
        public string SectionKey { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public int SortOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class SiteSettingResponse
    {
        public int Id { get; set; }
        public string Key { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public string ValueType { get; set; } = "text";
        public bool IsPublic { get; set; }
        public string? Description { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
