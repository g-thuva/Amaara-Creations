using System.ComponentModel.DataAnnotations;

namespace be.Domain.Cms;
public class CmsPage
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(160)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [StringLength(180)]
    public string Slug { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Summary { get; set; }
    public bool IsPublished { get; set; }
    public DateTime? PublishedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<CmsSection> Sections { get; set; } = new List<CmsSection>();
}
