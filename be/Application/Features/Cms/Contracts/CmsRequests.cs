using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Cms;
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
