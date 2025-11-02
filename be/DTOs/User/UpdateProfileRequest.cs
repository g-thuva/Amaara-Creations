using System.ComponentModel.DataAnnotations;

namespace be.DTOs.User
{
    public class UpdateProfileRequest
    {
        [Required]
        [StringLength(200, ErrorMessage = "Name must not exceed 200 characters")]
        public string Name { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "Phone must not exceed 50 characters")]
        public string? Phone { get; set; }

        [StringLength(500, ErrorMessage = "Address must not exceed 500 characters")]
        public string? Address { get; set; }

        [StringLength(500, ErrorMessage = "Avatar URL must not exceed 500 characters")]
        [Url(ErrorMessage = "Avatar URL must be a valid URL")]
        public string? AvatarUrl { get; set; }
    }
}

