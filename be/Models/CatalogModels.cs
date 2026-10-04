using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(140)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public int? ParentCategoryId { get; set; }

        public int? ImageMediaId { get; set; }

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ParentCategoryId")]
        public Category? ParentCategory { get; set; }

        public ICollection<Category> ChildCategories { get; set; } = new List<Category>();

        [ForeignKey("ImageMediaId")]
        public ProductMedia? ImageMedia { get; set; }

        public ICollection<Product> Products { get; set; } = new List<Product>();
    }

    public class Collection
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(140)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public int SortOrder { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProductCollection> ProductCollections { get; set; } = new List<ProductCollection>();
    }

    public class ProductCollection
    {
        public int ProductId { get; set; }

        public int CollectionId { get; set; }

        public int SortOrder { get; set; }

        public Product? Product { get; set; }

        public Collection? Collection { get; set; }
    }

    public class ProductVariant
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(64)]
        public string Sku { get; set; } = string.Empty;

        [Required]
        [StringLength(120)]
        public string Name { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PriceOverride { get; set; }

        public int StockQuantity { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [Timestamp]
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }

    public class ProductMedia
    {
        [Key]
        public int Id { get; set; }

        public int ProductId { get; set; }

        [Required]
        [StringLength(500)]
        public string StorageKey { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Url { get; set; }

        [Required]
        [StringLength(50)]
        public string MediaType { get; set; } = "image";

        [StringLength(200)]
        public string? AltText { get; set; }

        public int SortOrder { get; set; }

        public bool IsPrimary { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("ProductId")]
        public Product? Product { get; set; }
    }
}
