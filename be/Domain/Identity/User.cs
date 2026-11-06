using Microsoft.AspNetCore.Identity;

namespace be.Domain.Identity;
public class User : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public CustomerAccountStatus AccountStatus { get; set; } = CustomerAccountStatus.Active;
    public string? AccountStatusReason { get; set; }
    public DateTime? AccountStatusChangedAt { get; set; }
}
