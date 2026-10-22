using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Identity
{
    public class ConfirmEmailRequest
    {
        public string? UserId { get; set; }

        public string? Email { get; set; }

        public string? Token { get; set; }

        public string? Code { get; set; }
    }
}
