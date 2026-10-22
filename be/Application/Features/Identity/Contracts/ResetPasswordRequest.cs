using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Identity
{
    public class ResetPasswordRequest
    {
        public string? Token { get; set; }
        public string? Code { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MaxLength(256)]
        public string NewPassword { get; set; } = string.Empty;

        [Required]
        [Compare("NewPassword", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
