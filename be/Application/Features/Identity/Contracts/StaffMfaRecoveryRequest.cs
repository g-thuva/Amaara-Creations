using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Identity;
public sealed record StaffMfaRecoveryRequest([Required, EmailAddress] string Email, [Required, StringLength(300, MinimumLength = 10)] string Reason);
