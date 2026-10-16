using System.ComponentModel.DataAnnotations;

namespace be.Domain.Cms;
public class SiteSetting
{
    [Key]
    public int Id { get; set; }

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
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
