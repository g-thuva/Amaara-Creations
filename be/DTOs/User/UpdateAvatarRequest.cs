using System.ComponentModel.DataAnnotations;

namespace be.DTOs.User
{
    public class UpdateAvatarRequest
    {
        [StringLength(500, ErrorMessage = "Avatar URL must not exceed 500 characters")]
        [Url(ErrorMessage = "Avatar URL must be a valid URL")]
        public string? AvatarUrl { get; set; }
    }
}

