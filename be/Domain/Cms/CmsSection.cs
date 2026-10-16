using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Cms;
public class CmsSection
{
    [Key]
    public int Id { get; set; }
    public int CmsPageId { get; set; }

    [Required]
    [StringLength(120)]
    public string SectionKey { get; set; } = string.Empty;

    [Required]
    [StringLength(80)]
    public string ContentType { get; set; } = "text";

    [Required]
    public string Content { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("CmsPageId")]
    public CmsPage? Page { get; set; }
}
