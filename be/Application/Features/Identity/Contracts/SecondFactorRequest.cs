using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Identity;
public class SecondFactorRequest
{
    [Required, StringLength(256)] public string Password { get; set; } = string.Empty;
    [StringLength(16)] public string? AuthenticatorCode { get; set; }
    [StringLength(64)] public string? RecoveryCode { get; set; }
}
