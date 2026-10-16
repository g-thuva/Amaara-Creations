using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Identity
{
    public class ForgotPasswordRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
