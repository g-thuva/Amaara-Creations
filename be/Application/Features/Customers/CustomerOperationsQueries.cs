using be.Domain.Common;
using be.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;

namespace be.Application.Features.Customers;
public sealed class CustomerOperationsQueries(IApplicationDbContext db)
{
    public async Task<object?> Customer(string id, CancellationToken ct)
    {
        var customerRole = await db.Roles.Where(r => r.Name == AppRoles.Customer).Select(r => r.Id).SingleOrDefaultAsync(ct);
        var customer = await db.Users.AsNoTracking()
            .Where(u => u.Id == id)
            .Select(u => new { u.Id, u.Name, u.Email, u.Phone, u.CreatedAt, u.EmailConfirmed, AccountStatus = u.AccountStatus.ToString(), u.AccountStatusReason, u.AccountStatusChangedAt, Version = u.ConcurrencyStamp, CanManageAccess = !db.UserRoles.Any(assignment => assignment.UserId == u.Id && assignment.RoleId != customerRole), Locked = u.LockoutEnabled && u.LockoutEnd > DateTimeOffset.UtcNow })
            .SingleOrDefaultAsync(ct);
        if (customer == null)
            throw new CommerceException("REPORTING_NOT_FOUND", "Resource not found.", 404);
        if (!await db.UserRoles.AnyAsync(r => r.UserId == id && r.RoleId == customerRole, ct))
            throw new CommerceException("REPORTING_NOT_FOUND", "Resource not found.", 404);
        return new
        {
            customer,
            orderCount = await db.Orders.CountAsync(o => o.UserId == id, ct),
            lifetimeValue = await db.Payments.AsNoTracking()
            .Where(p => p.Order!.UserId == id && p.SettledAmount > 0)
            .GroupBy(p => new { p.Currency, p.ProviderMode })
            .Select(g => new { g.Key.Currency, g.Key.ProviderMode, Settled = g.Sum(p => p.SettledAmount), Refunded = g.Sum(p => p.RefundedAmount) })
            .ToListAsync(ct),
            addresses = await db.Addresses.AsNoTracking()
            .Where(a => a.UserId == id)
            .Select(a => new { a.Id, a.Label, a.RecipientName, a.AddressLine1, a.AddressLine2, a.City, a.DistrictOrProvince, a.PostalCode, a.Country, a.Phone, a.IsDefault })
            .ToListAsync(ct),
            orders = await db.Orders.AsNoTracking()
            .Where(o => o.UserId == id)
            .OrderByDescending(o => o.Id)
            .Take(50)
            .Select(o => new { o.Id, o.OrderNumber, o.OrderDate, o.Currency, o.Total, Status = o.Status.ToString() })
            .ToListAsync(ct),
            designs = await db.CustomDesigns.AsNoTracking()
            .Where(d => d.UserId == id)
            .OrderByDescending(d => d.Id)
            .Take(50)
            .Select(d => new { d.Id, d.Name, Status = d.Status.ToString(), ProductionStatus = d.ProductionStatus.ToString(), ProofStatus = d.ProofStatus.ToString() })
            .ToListAsync(ct),
            activity = await db.AuditLogs.AsNoTracking().Where(a => a.EntityType == "User" && a.EntityId == id && a.Action == "CustomerAccountStatusChanged")
                .OrderByDescending(a => a.Id).Take(30)
                .Select(a => new { a.Action, a.CreatedAt, ActorName = a.ActorUser != null ? a.ActorUser.Name : "System", a.OldValuesJson, a.NewValuesJson }).ToListAsync(ct),
            removalRequests = await db.CustomerRequests.AsNoTracking().Where(r => r.UserId == id && r.Kind == CustomerRequestKind.PrivacyDeletion)
                .OrderByDescending(r => r.CreatedAt).Take(10)
                .Select(r => new { r.Id, Status = r.Status.ToString(), r.CreatedAt }).ToListAsync(ct),
            reviews = await db.Reviews.AsNoTracking()
            .Where(r => r.UserId == id)
            .OrderByDescending(r => r.Id)
            .Take(50)
            .Select(r => new { r.Id, r.ProductId, r.Rating, r.Comment, r.ModerationStatus, r.CreatedAt })
            .ToListAsync(ct)
        };
    }
}
