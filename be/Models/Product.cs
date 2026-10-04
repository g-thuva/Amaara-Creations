using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Models
{
    public class Product
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(220)]
        public string Slug { get; set; } = string.Empty;

        [StringLength(300)]
        public string? ShortDescription { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Price { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BasePrice { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = "custom"; // wedding, car, custom

        public int? CategoryId { get; set; }

        [StringLength(64)]
        public string? BaseSku { get; set; }

        [Required]
        public int Stock { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [ForeignKey("CategoryId")]
        public Category? CategoryEntity { get; set; }

        public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

        public ICollection<ProductMedia> Media { get; set; } = new List<ProductMedia>();

        public ICollection<ProductCollection> ProductCollections { get; set; } = new List<ProductCollection>();
    }
}

