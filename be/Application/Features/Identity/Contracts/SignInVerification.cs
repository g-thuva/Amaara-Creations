using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Identity;
public sealed record SignInVerification([StringLength(16)] string? Code, [StringLength(64)] string? RecoveryCode);
public sealed record SignInAcknowledgment(bool SavedRecoveryCodes);
