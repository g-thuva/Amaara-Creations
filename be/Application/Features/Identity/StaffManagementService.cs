using be.Domain.Common;
using be.Application.Abstractions.Audit;
using be.Domain.Identity;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;

namespace be.Application.Features.Identity;
public sealed class StaffManagementService(IApplicationDbContext db, UserManager<User> users, IAuditService audit, IRequestActor _actor)
{
    public async Task<object?> Staff(int page = 1, CancellationToken ct = default)
    {
        var roles = await db.Roles.Where(r => AppRoles.AdministrativeRoles.Contains(r.Name!)).ToListAsync(ct);
        var roleIds = roles.Select(r => r.Id).ToArray();
        var staffIds = db.UserRoles.Where(r => roleIds.Contains(r.RoleId)).Select(r => r.UserId);
        var staff = await db.Users.Where(u => staffIds.Contains(u.Id))
            .OrderBy(u => u.Id)
            .Skip((Math.Max(1, page) - 1) * 50)
            .Take(50)
            .Select(u => new { u.Id, u.Name, u.Email })
            .ToListAsync(ct);
        var ids = staff.Select(s => s.Id).ToArray();
        var assignments = await db.UserRoles.Where(r => ids.Contains(r.UserId)).ToListAsync(ct);
        return new
        {
            roles = AppRoles.AdministrativeRoles,
            items = staff.Select(s => new { s.Id, s.Name, s.Email, Roles = roles.Where(r => assignments.Any(a => a.UserId == s.Id && a.RoleId == r.Id)).Select(r => r.Name) })
        };
    }

    public async Task Assign(StaffRequest request, CancellationToken ct)
    {
        if (request.Roles == null || request.Roles.Any(r => !AppRoles.AdministrativeRoles.Contains(r)) || string.IsNullOrWhiteSpace(request.Reason))
            throw new CommerceException("STAFF_INVALID", "Choose valid roles and provide a reason.");
        await using var tx = await db.BeginCommerceTransactionAsync(ct);
        var user = await users.FindByEmailAsync(request.Email) ?? throw new CommerceException("USER_NOT_FOUND", "Create and verify the account before assigning staff access.", 404);
        if (!user.EmailConfirmed)
            throw new CommerceException("STAFF_EMAIL_UNVERIFIED", "Staff must verify their email before access is assigned.", 409);
        if (user.Id == _actor.User.FindFirstValue(ClaimTypes.NameIdentifier))
            throw new CommerceException("STAFF_SELF_CHANGE", "A second Super Admin must change your roles.", 409);
        var previous = await users.GetRolesAsync(user);
        if (previous.Contains(AppRoles.SuperAdmin) && !request.Roles.Contains(AppRoles.SuperAdmin))
        {
            var superRole = await db.Roles.SingleAsync(r => r.Name == AppRoles.SuperAdmin, ct);
            if (await db.UserRoles.CountAsync(r => r.RoleId == superRole.Id, ct) <= 1)
                throw new CommerceException("LAST_SUPER_ADMIN", "The last Super Admin cannot be removed.", 409);
        }

        var desired = request.Roles.Distinct().ToArray();
        var remove = await users.RemoveFromRolesAsync(user, previous.Where(AppRoles.AdministrativeRoles.Contains).Except(desired));
        var add = await users.AddToRolesAsync(user, desired.Except(previous));
        if (!remove.Succeeded || !add.Succeeded)
            throw new CommerceException("STAFF_UPDATE_FAILED", "Staff roles could not be saved.", 409);
        foreach (var session in await db.RefreshSessions.Where(s => s.UserId == user.Id && s.RevokedAt == null).ToListAsync(ct))
            session.RevokedAt = DateTime.UtcNow;
        await audit.RecordAsync("StaffRolesChanged", "User", user.Id, new { Roles = previous }, new { Roles = desired, request.Reason }, _actor.Audit, ct);
        if (tx != null)
            await tx.CommitAsync(ct);
        return;
    }
}
