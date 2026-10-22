using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Persistence;
using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;

namespace be.Application.Features.Identity;
// Operator tool only. Never exposed by an HTTP endpoint or startup bootstrap.
public sealed class IdentityRoleProvisioner(UserManager<User> users, RoleManager<IdentityRole> roles, IApplicationDbContext db, IRefreshSessionService sessions, IAuditService audit)
{
    public async Task<bool> Assign(string email, string role, string environment, bool explicitlyApproved, string reason, CancellationToken ct)
    {
        if (!explicitlyApproved || environment is not ("Development" or "Staging")) throw new InvalidOperationException("Role provisioning requires explicit approval in Development or Staging; Production is refused.");
        if (!AppRoles.AdministrativeRoles.Contains(role) || string.IsNullOrWhiteSpace(reason) || reason.Length > 300) throw new InvalidOperationException("Choose an exact canonical administrative role and a bounded audit reason.");
        var user = await users.FindByEmailAsync(email) ?? throw new InvalidOperationException("The explicitly selected existing identity was not found.");
        if (!user.EmailConfirmed) throw new InvalidOperationException("Verify the existing account email through the normal process first.");
        if (await users.IsInRoleAsync(user, role)) return false;
        await using var transaction = await db.BeginCommerceTransactionAsync(ct);
        if (!await roles.RoleExistsAsync(role)) Ensure(await roles.CreateAsync(new IdentityRole(role)));
        Ensure(await users.AddToRoleAsync(user, role));
        Ensure(await users.UpdateSecurityStampAsync(user));
        await sessions.RevokeAllForUserAsync(user.Id, ct);
        await audit.RecordAsync("OperatorStaffRoleAssigned", nameof(User), user.Id, null, new { Role = role, Environment = environment, Reason = reason }, new(null, "identity-admin-cli", null), ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        return true;
    }
    private static void Ensure(IdentityResult result)
    {
        if (!result.Succeeded) throw new InvalidOperationException("Identity operation failed: " + string.Join(",", result.Errors.Select(e => e.Code)));
    }
}
