namespace be.Application.Features.Cms;
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
