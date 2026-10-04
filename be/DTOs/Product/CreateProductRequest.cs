using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Product
{
    public class CreateProductRequest
    {
        [Required]
        [StringLength(200)]
        public string Name { get; set; } = string.Empty;

        [StringLength(220)]
        public string? Slug { get; set; }

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Base price must be greater than 0")]
        public decimal? BasePrice { get; set; }

        [Required]
        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [StringLength(300)]
        public string? ShortDescription { get; set; }

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
        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int Stock { get; set; } = 0;

        public bool IsFeatured { get; set; }
    }
}

