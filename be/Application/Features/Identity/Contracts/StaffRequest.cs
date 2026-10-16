using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Identity;
public sealed record StaffRequest([Required, EmailAddress] string Email, string[] Roles, [Required, StringLength(500)] string Reason);
