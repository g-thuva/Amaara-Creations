using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public class InventoryTransaction
    {
        [Key]
        public long Id { get; set; }

        public int? ProductId { get; set; }

        public int? ProductVariantId { get; set; }

        public int PreviousQuantity { get; set; }

        public int QuantityDelta { get; set; }

        public int NewQuantity { get; set; }

        [Required]
        [StringLength(300)]
        public string Reason { get; set; } = string.Empty;

        public string? PerformedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }

        [ForeignKey("ProductVariantId")]
        public ProductVariant? ProductVariant { get; set; }

        [ForeignKey("PerformedByUserId")]
        public User? PerformedByUser { get; set; }
    }

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
}
