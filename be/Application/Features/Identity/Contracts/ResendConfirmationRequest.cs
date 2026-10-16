using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Identity
{
    public class ResendConfirmationRequest
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }
}
