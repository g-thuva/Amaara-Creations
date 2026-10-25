using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Identity
{
    public class UpdateAvatarRequest
    {
        [StringLength(500, ErrorMessage = "Avatar URL must not exceed 500 characters")]
        [AvatarMediaUrl(ErrorMessage = "Avatar URL must be a valid public image URL")]
        public string? AvatarUrl { get; set; }
    }
}
