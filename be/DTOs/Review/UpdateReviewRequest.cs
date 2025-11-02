using System.ComponentModel.DataAnnotations;

namespace be.DTOs.Review
{
    public class UpdateReviewRequest
    {
        [Required]
        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
        public int Rating { get; set; }

        [StringLength(1000, ErrorMessage = "Comment must not exceed 1000 characters")]
        public string? Comment { get; set; }
    }
}

