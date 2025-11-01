using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Product
{
    public class UpdateProductRequest
    {
        [StringLength(200)]
        public string? Name { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than 0")]
        public decimal? Price { get; set; }

        [StringLength(500)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? ImageUrl { get; set; }

        [StringLength(50)]
        public string? Category { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Stock cannot be negative")]
        public int? Stock { get; set; }

        public bool? IsActive { get; set; }
    }
}

