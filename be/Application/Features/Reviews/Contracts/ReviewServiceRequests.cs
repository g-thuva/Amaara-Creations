using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Reviews;
public sealed record ModerationRequest(string Status, [Required, StringLength(500)] string Reason, [Required(AllowEmptyStrings = true)] string RowVersion);
