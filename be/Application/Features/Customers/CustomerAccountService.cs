using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Persistence;
using be.Domain.Common;
using be.Domain.Identity;
using Microsoft.EntityFrameworkCore;

namespace be.Application.Features.Customers;

public sealed class CustomerAccountRequest
{
    [Required, RegularExpression("^(Active|Suspended|Blocked|PendingDeletion|Removed)$")]
    public string Status { get; set; } = string.Empty;
    [Required, StringLength(500, MinimumLength = 10)] public string Reason { get; set; } = string.Empty;
    [Required, StringLength(100)] public string Version { get; set; } = string.Empty;
    // Removal retains the account's business records; PendingDeletion requests a separate privacy review.
    public bool Acknowledged { get; set; }
}

public sealed class CustomerAccountService(IApplicationDbContext db, IAuditService audit, IRequestActor actor)
{
    public async Task Change(string id, CustomerAccountRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<CustomerAccountStatus>(request.Status, out var next) || !Enum.IsDefined(next)
            || string.IsNullOrWhiteSpace(request.Reason) || request.Reason.Trim().Length is < 10 or > 500)
            throw new CommerceException("CUSTOMER_ACTION_INVALID", "Choose an account state and explain the action in 10 to 500 characters.");
        if (!actor.User.IsInRole(AppRoles.Admin) && !actor.User.IsInRole(AppRoles.SuperAdmin))
            throw new CommerceException("CUSTOMER_ACTION_DENIED", "You cannot change customer access.", 403);
        await using var tx = await db.BeginCommerceTransactionAsync(ct);
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == id, ct)
            ?? throw new CommerceException("CUSTOMER_NOT_FOUND", "Customer not found.", 404);
        var roles = await (from assignment in db.UserRoles join role in db.Roles on assignment.RoleId equals role.Id
                           where assignment.UserId == id select role.Name).ToListAsync(ct);
        if (!roles.Contains(AppRoles.Customer) || roles.Any(r => r != AppRoles.Customer) || id == actor.User.FindFirstValue(ClaimTypes.NameIdentifier))
            throw new CommerceException("CUSTOMER_ACTION_DENIED", "This action is only available for customer accounts.", 403);
        if (next == CustomerAccountStatus.PendingDeletion || user.AccountStatus == CustomerAccountStatus.PendingDeletion)
        {
            if (!actor.User.IsInRole(AppRoles.SuperAdmin))
                throw new CommerceException("CUSTOMER_REMOVAL_DENIED", "An owner must review account removal or restoration.", 403);
            if (next == CustomerAccountStatus.PendingDeletion && !request.Acknowledged)
                throw new CommerceException("CUSTOMER_REMOVAL_ACKNOWLEDGMENT", "Acknowledge that removal is reviewed and historical business records are retained.");
        }
        if (next == CustomerAccountStatus.Removed && !request.Acknowledged)
            throw new CommerceException("CUSTOMER_REMOVAL_ACKNOWLEDGMENT", "Acknowledge that the customer loses access and historical business records are retained.");
        if (string.IsNullOrEmpty(request.Version) || user.ConcurrencyStamp != request.Version)
            throw new CommerceException("CUSTOMER_CHANGED", "The account changed. Reload it before trying again.", 409);
        if (user.AccountStatus == next)
            throw new CommerceException("CUSTOMER_STATE_UNCHANGED", "The account already has this status.", 409);
        if (user.AccountStatus == CustomerAccountStatus.Removed && next is CustomerAccountStatus.Suspended or CustomerAccountStatus.Blocked)
            throw new CommerceException("CUSTOMER_TRANSITION_INVALID", "Reactivate the removed customer before changing their access status.", 409);
        var previous = user.AccountStatus;
        user.AccountStatus = next;
        user.AccountStatusReason = request.Reason.Trim();
        user.AccountStatusChangedAt = user.UpdatedAt = DateTime.UtcNow;
        user.ConcurrencyStamp = Guid.NewGuid().ToString();
        user.SecurityStamp = Guid.NewGuid().ToString();
        // Status, revocations, review request and audit share the same unit of work.
        var sessions = await db.RefreshSessions.Where(s => s.UserId == id && s.RevokedAt == null).ToListAsync(ct);
        foreach (var session in sessions)
        {
            session.RevokedAt = DateTime.UtcNow;
            session.RevocationReason = "customer-access-changed";
        }
        if (next == CustomerAccountStatus.PendingDeletion && !await db.CustomerRequests.AnyAsync(r => r.UserId == id
            && r.Kind == CustomerRequestKind.PrivacyDeletion && r.Status != CustomerRequestStatus.Closed && r.Status != CustomerRequestStatus.Cancelled, ct))
            db.CustomerRequests.Add(new CustomerRequest { UserId = id, Kind = CustomerRequestKind.PrivacyDeletion,
                Subject = "Administrator-initiated account removal review",
                // Requests are visible to their customer after reactivation; staff evidence belongs only in the restricted audit.
                Details = "An administrator requested an account-removal review. Identity, retention and outstanding business records must be checked before any separate removal process." });
        await audit.RecordAsync("CustomerAccountStatusChanged", nameof(User), id, new { Status = previous.ToString() },
            new { Status = next.ToString(), Reason = user.AccountStatusReason, RevokedSessions = sessions.Count, Outcome = "Succeeded" }, actor.Audit, ct);
        if (tx != null) await tx.CommitAsync(ct);
    }
}
