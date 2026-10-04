using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Product
{
    public class UpdateProductRequest
    {
        [StringLength(200)]
        public string? Name { get; set; }

        [StringLength(220)]
        public string? Slug { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal? Price { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Base price must be greater than 0")]
        public decimal? BasePrice { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(300)]
        public string? ShortDescription { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        public int? CategoryId { get; set; }

        [StringLength(64)]
        public string? BaseSku { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int? Stock { get; set; }

        public bool? IsActive { get; set; }

        public bool? IsFeatured { get; set; }
    }
}

