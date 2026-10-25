using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace be.Application.Features.Identity;
public sealed class AvatarMediaUrlAttribute : ValidationAttribute
{
    public override bool IsValid(object? value)
    {
        if (value is not string url || url.Length == 0) return true;
        if (url.Any(char.IsControl) || url.Contains('\\')) return false;
        if (Regex.IsMatch(url, "\\A/api/v1/media/public/[a-z][a-z0-9-]*/(?:[0-9]{4}/(?:0[1-9]|1[0-2])/)?[a-f0-9]{32}\\.png\\z")) return true;
        if (Regex.IsMatch(url, "\\A/uploads/avatars/[a-zA-Z0-9_-]{1,180}\\.(?:png|jpe?g|gif|webp)\\z")) return true;
        return Uri.TryCreate(url, UriKind.Absolute, out var absolute) && absolute.Scheme is "http" or "https" && absolute.UserInfo.Length == 0 && !absolute.Query.Contains("X-Amz-", StringComparison.OrdinalIgnoreCase);
    }
}
