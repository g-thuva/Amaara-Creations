using System.Security.Claims;
using System.ComponentModel.DataAnnotations;
using System.Text;
using be.Data;
using be.Models;
using be.Security;
using be.Services;
using be.Services.Checkout;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController, Route("api/v1/admin"), Authorize(Policy = AppPolicies.AdminAccess)]
public sealed class AdminOperationsController(ApplicationDbContext db, UserManager<User> users, IAuditService audit) : ControllerBase
{
    [HttpGet("capabilities")]
    public IActionResult Capabilities() => Ok(AppPolicies.Roles.Where(p => p.Value.Any(User.IsInRole)).Select(p => p.Key));

    [HttpGet("payments"), Authorize(Policy = AppPolicies.ManagePayments)]
    public async Task<IActionResult> Payments(string? status, string? mode, int page = 1, CancellationToken ct = default)
    {
        var query = db.Payments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PaymentStatus>(status, out var state) || !Enum.IsDefined(state)) throw new CommerceException("PAYMENT_STATUS_INVALID", "Choose a valid payment state.");
            query = query.Where(p => p.Status == state);
        }
        if (!string.IsNullOrWhiteSpace(mode)) query = query.Where(p => p.ProviderMode == mode);
        return Ok(new { items = await query.OrderByDescending(p => p.Id).Skip((Math.Max(1, page) - 1) * 30).Take(30)
            .Select(p => new { p.Id, p.OrderId, p.Order!.OrderNumber, p.Provider, p.ProviderMode, p.Amount, p.Currency, Status = p.Status.ToString(), p.ProviderReference, p.ProviderGatewayId, p.CreatedAt, p.PaidAt, p.LastReconciledAt }).ToListAsync(ct), totalCount = await query.CountAsync(ct) });
    }

    [HttpGet("customers/{id}/operations"), Authorize(Policy = AppPolicies.ManageCustomers)]
    public async Task<IActionResult> Customer(string id, CancellationToken ct)
    {
        var customer = await db.Users.AsNoTracking().Where(u => u.Id == id).Select(u => new { u.Id, u.Name, u.Email, u.Phone, u.CreatedAt, u.EmailConfirmed,
            Locked = u.LockoutEnabled && u.LockoutEnd > DateTimeOffset.UtcNow }).SingleOrDefaultAsync(ct);
        if (customer == null) return NotFound();
        var customerRole = await db.Roles.Where(r => r.Name == AppRoles.Customer).Select(r => r.Id).SingleOrDefaultAsync(ct);
        if (!await db.UserRoles.AnyAsync(r => r.UserId == id && r.RoleId == customerRole, ct)) return NotFound();
        return Ok(new
        {
            customer,
            orderCount = await db.Orders.CountAsync(o => o.UserId == id, ct),
            lifetimeValue = await db.Payments.AsNoTracking().Where(p => p.Order!.UserId == id && p.SettledAmount > 0)
                .GroupBy(p => new { p.Currency, p.ProviderMode }).Select(g => new { g.Key.Currency, g.Key.ProviderMode, Settled = g.Sum(p => p.SettledAmount), Refunded = g.Sum(p => p.RefundedAmount) }).ToListAsync(ct),
            addresses = await db.Addresses.AsNoTracking().Where(a => a.UserId == id).Select(a => new { a.Id, a.Label, a.RecipientName, a.AddressLine1, a.AddressLine2, a.City, a.DistrictOrProvince, a.PostalCode, a.Country, a.Phone, a.IsDefault }).ToListAsync(ct),
            orders = await db.Orders.AsNoTracking().Where(o => o.UserId == id).OrderByDescending(o => o.Id).Take(50).Select(o => new { o.Id, o.OrderNumber, o.OrderDate, o.Currency, o.Total, Status = o.Status.ToString() }).ToListAsync(ct),
            designs = await db.CustomDesigns.AsNoTracking().Where(d => d.UserId == id).OrderByDescending(d => d.Id).Take(50).Select(d => new { d.Id, d.Name, Status = d.Status.ToString(), ProductionStatus = d.ProductionStatus.ToString(), ProofStatus = d.ProofStatus.ToString() }).ToListAsync(ct),
            reviews = await db.Reviews.AsNoTracking().Where(r => r.UserId == id).OrderByDescending(r => r.Id).Take(50).Select(r => new { r.Id, r.ProductId, r.Rating, r.Comment, r.ModerationStatus, r.CreatedAt }).ToListAsync(ct)
        });
    }

    [HttpGet("audit"), Authorize(Policy = AppPolicies.ViewAudit)]
    public async Task<IActionResult> Audit(string? action, string? entityType, string? entityId, int page = 1, CancellationToken ct = default)
    {
        var query = db.AuditLogs.AsNoTracking().Where(a => (action == null || a.Action == action) && (entityType == null || a.EntityType == entityType) && (entityId == null || a.EntityId == entityId));
        return Ok(new { items = await query.OrderByDescending(a => a.Id).Skip((Math.Max(1, page) - 1) * 50).Take(50)
            .Select(a => new { a.Id, a.ActorUserId, a.Action, a.EntityType, a.EntityId, a.CreatedAt, a.CorrelationId, a.OldValuesJson, a.NewValuesJson }).ToListAsync(ct), totalCount = await query.CountAsync(ct) });
    }

    [HttpGet("reports"), Authorize(Policy = AppPolicies.ViewReports)]
    public async Task<IActionResult> Reports(DateTime? from, DateTime? to, CancellationToken ct)
    {
        ValidateDates(from, to);
        var orders = db.Orders.AsNoTracking().Where(o => (!from.HasValue || o.OrderDate >= from) && (!to.HasValue || o.OrderDate < to));
        var payments = db.Payments.AsNoTracking().Where(p => (!from.HasValue || p.CreatedAt >= from) && (!to.HasValue || p.CreatedAt < to));
        var soldOrders = orders.Where(o => o.FinalizedAt != null && o.Status != OrderStatus.Cancelled && o.Payments.Any(p => p.SettledAmount > 0));
        return Ok(new
        {
            from, to,
            money = await payments.GroupBy(p => new { p.Currency, p.ProviderMode }).Select(g => new { g.Key.Currency, g.Key.ProviderMode, Attempts = g.Count(), Settled = g.Sum(p => p.SettledAmount), Refunded = g.Sum(p => p.RefundedAmount) }).ToListAsync(ct),
            orders = await orders.GroupBy(o => new { o.Status, o.Currency }).Select(g => new { Status = g.Key.Status.ToString(), g.Key.Currency, Count = g.Count(), Total = g.Sum(o => o.Total) }).ToListAsync(ct),
            products = await soldOrders.SelectMany(o => o.OrderItems).GroupBy(i => new { i.ProductNameSnapshot, i.ItemType, i.Order!.Currency }).Select(g => new { Product = g.Key.ProductNameSnapshot, Type = g.Key.ItemType.ToString(), g.Key.Currency, Quantity = g.Sum(i => i.Quantity), Total = g.Sum(i => i.Subtotal) }).ToListAsync(ct),
            production = await db.CustomDesigns.Where(d => d.Status == CustomDesignStatus.Ordered).GroupBy(d => d.ProductionStatus).Select(g => new { State = g.Key.ToString(), Count = g.Count() }).ToListAsync(ct),
            inventory = await db.InventoryTransactions.Where(m => (!from.HasValue || m.CreatedAt >= from) && (!to.HasValue || m.CreatedAt < to)).GroupBy(m => m.MovementType).Select(g => new { Type = g.Key, Count = g.Count(), Delta = g.Sum(m => m.QuantityDelta) }).ToListAsync(ct),
            promotions = await db.PromotionRedemptions.Where(r => (!from.HasValue || r.CreatedAt >= from) && (!to.HasValue || r.CreatedAt < to)).GroupBy(r => new { r.Promotion.Code, r.Promotion.Currency, r.Status }).Select(g => new { g.Key.Code, g.Key.Currency, g.Key.Status, Count = g.Count(), Discount = g.Sum(r => r.Amount) }).ToListAsync(ct)
        });
    }

    [HttpGet("reports/orders.csv"), Authorize(Policy = AppPolicies.ViewReports)]
    public async Task<IActionResult> Export(DateTime? from, DateTime? to, CancellationToken ct)
    {
        ValidateDates(from, to);
        var query = db.Orders.AsNoTracking().Where(o => (!from.HasValue || o.OrderDate >= from) && (!to.HasValue || o.OrderDate < to));
        if (await query.CountAsync(ct) > 10000) throw new CommerceException("EXPORT_TOO_LARGE", "Narrow the date range to at most 10,000 orders.");
        var rows = await query.OrderBy(o => o.Id).Select(o => new { o.OrderNumber, o.OrderDate, o.Status, o.Currency, o.Subtotal, o.DiscountTotal, o.ShippingTotal, o.Total }).ToListAsync(ct);
        var csv = new StringBuilder("order_number,ordered_at_utc,status,currency,subtotal,discount,shipping,total\r\n");
        foreach (var r in rows) csv.AppendLine(string.Join(",", new[] { r.OrderNumber, r.OrderDate.ToString("O"), r.Status.ToString(), r.Currency,
            r.Subtotal.ToString(System.Globalization.CultureInfo.InvariantCulture), r.DiscountTotal.ToString(System.Globalization.CultureInfo.InvariantCulture), r.ShippingTotal.ToString(System.Globalization.CultureInfo.InvariantCulture), r.Total.ToString(System.Globalization.CultureInfo.InvariantCulture) }.Select(CsvCell)));
        await audit.RecordAsync("ReportExported", "Order", null, null, new { from, to, Count = rows.Count }, HttpContext, ct);
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv; charset=utf-8", "orders.csv");
    }

    public static string CsvCell(string value)
    {
        if (value.TrimStart().StartsWith('=') || value.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-') || value.TrimStart().StartsWith('@') || value.StartsWith('\t') || value.StartsWith('\r')) value = "'" + value;
        return "\"" + value.Replace("\"", "\"\"") + "\"";
    }
    private static void ValidateDates(DateTime? from, DateTime? to)
    {
        if (from.HasValue && to.HasValue && from >= to) throw new CommerceException("DATE_RANGE_INVALID", "The end date must follow the start date (exclusive).");
    }

    [HttpGet("staff"), Authorize(Policy = AppPolicies.ManageStaff)]
    public async Task<IActionResult> Staff(int page = 1, CancellationToken ct = default)
    {
        var roles = await db.Roles.Where(r => AppRoles.AdministrativeRoles.Contains(r.Name!)).ToListAsync(ct);
        var roleIds = roles.Select(r => r.Id).ToArray();
        var staffIds = db.UserRoles.Where(r => roleIds.Contains(r.RoleId)).Select(r => r.UserId);
        var staff = await db.Users.Where(u => staffIds.Contains(u.Id)).OrderBy(u => u.Id).Skip((Math.Max(1, page) - 1) * 50).Take(50).Select(u => new { u.Id, u.Name, u.Email }).ToListAsync(ct);
        var ids = staff.Select(s => s.Id).ToArray();
        var assignments = await db.UserRoles.Where(r => ids.Contains(r.UserId)).ToListAsync(ct);
        return Ok(new { roles = AppRoles.AdministrativeRoles, items = staff.Select(s => new { s.Id, s.Name, s.Email, Roles = roles.Where(r => assignments.Any(a => a.UserId == s.Id && a.RoleId == r.Id)).Select(r => r.Name) }) });
    }

    public sealed record StaffRequest([Required, EmailAddress] string Email, string[] Roles, [Required, StringLength(500)] string Reason);
    [HttpPut("staff"), Authorize(Policy = AppPolicies.ManageStaff)]
    public async Task<IActionResult> Assign(StaffRequest request, CancellationToken ct)
    {
        if (request.Roles == null || request.Roles.Any(r => !AppRoles.AdministrativeRoles.Contains(r)) || string.IsNullOrWhiteSpace(request.Reason))
            throw new CommerceException("STAFF_INVALID", "Choose valid roles and provide a reason.");
        await using var tx = await CommerceTransaction.BeginAsync(db, ct);
        var user = await users.FindByEmailAsync(request.Email) ?? throw new CommerceException("USER_NOT_FOUND", "Create and verify the account before assigning staff access.", 404);
        if (!user.EmailConfirmed) throw new CommerceException("STAFF_EMAIL_UNVERIFIED", "Staff must verify their email before access is assigned.", 409);
        if (user.Id == User.FindFirstValue(ClaimTypes.NameIdentifier)) throw new CommerceException("STAFF_SELF_CHANGE", "A second Super Admin must change your roles.", 409);
        var previous = await users.GetRolesAsync(user);
        if (previous.Contains(AppRoles.SuperAdmin) && !request.Roles.Contains(AppRoles.SuperAdmin))
        {
            var superRole = await db.Roles.SingleAsync(r => r.Name == AppRoles.SuperAdmin, ct);
            if (await db.UserRoles.CountAsync(r => r.RoleId == superRole.Id, ct) <= 1) throw new CommerceException("LAST_SUPER_ADMIN", "The last Super Admin cannot be removed.", 409);
        }
        var desired = request.Roles.Distinct().ToArray();
        var remove = await users.RemoveFromRolesAsync(user, previous.Where(AppRoles.AdministrativeRoles.Contains).Except(desired));
        var add = await users.AddToRolesAsync(user, desired.Except(previous));
        if (!remove.Succeeded || !add.Succeeded) throw new CommerceException("STAFF_UPDATE_FAILED", "Staff roles could not be saved.", 409);
        foreach (var session in await db.RefreshSessions.Where(s => s.UserId == user.Id && s.RevokedAt == null).ToListAsync(ct)) session.RevokedAt = DateTime.UtcNow;
        await audit.RecordAsync("StaffRolesChanged", "User", user.Id, new { Roles = previous }, new { Roles = desired, request.Reason }, HttpContext, ct);
        if (tx != null) await tx.CommitAsync(ct);
        return NoContent();
    }
}
