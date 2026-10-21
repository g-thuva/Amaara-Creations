using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Persistence;
using be.Domain.Common;
using be.Domain.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace be.Application.Features.Identity;
public sealed class CreateCustomerRequest
{
    [Required, StringLength(160)] public string Subject { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Details { get; set; } = string.Empty;
    [Range(1, 100000)] public int? Quantity { get; set; }
    public int? CustomDesignId { get; set; }
}
public sealed class ReviewCustomerRequest
{
    [Required, RegularExpression("^(InReview|Responded|Closed)$")] public string Status { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Reply { get; set; } = string.Empty;
    public string? RowVersion { get; set; }
}
public sealed class CustomerRequestService(IApplicationDbContext db, IRequestActor actor, IConfiguration configuration, IAuditService audit, IAuthorizationService authorization)
{
    private string UserId => actor.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new CommerceException("AUTHENTICATION_REQUIRED", "Please sign in.", 401);
    public object Capabilities() => new { quotesEnabled = configuration.GetValue<bool>("CustomerRequests:QuotesEnabled"), deletionRequestsEnabled = configuration.GetValue<bool>("Privacy:RequestsEnabled"), emailChangeEnabled = configuration.GetValue<bool>("Authentication:EmailChangeEnabled") };
    public async Task<object> Own(int page, CancellationToken ct)
    {
        page = Math.Clamp(page, 1, 100000);
        var query = db.CustomerRequests.AsNoTracking().Where(x => x.UserId == UserId);
        var count = await query.CountAsync(ct);
        var rows = await query.OrderByDescending(x => x.CreatedAt).Skip((page - 1) * 20).Take(20).ToListAsync(ct);
        return new { items = rows.Select(Dto), totalCount = count, page, totalPages = (int)Math.Ceiling(count / 20m) };
    }
    public async Task<object> Create(CustomerRequestKind kind, CreateCustomerRequest request, CancellationToken ct)
    {
        var enabled = configuration.GetValue<bool>(kind == CustomerRequestKind.CustomQuote ? "CustomerRequests:QuotesEnabled" : "Privacy:RequestsEnabled");
        if (!enabled) throw new CommerceException("REQUESTS_DISABLED", "This online request service is not enabled.", 503);
        if (string.IsNullOrWhiteSpace(request.Subject) || string.IsNullOrWhiteSpace(request.Details)) throw new CommerceException("REQUEST_INVALID", "Add a subject and request details.");
        await using var transaction = await db.BeginCommerceTransactionAsync(ct);
        if (await db.CustomerRequests.CountAsync(x => x.UserId == UserId && x.Kind == kind && x.Status != CustomerRequestStatus.Closed && x.Status != CustomerRequestStatus.Cancelled, ct) >= 10)
            throw new CommerceException("REQUEST_LIMIT", "Review your open requests before sending another.", 409);
        if (request.CustomDesignId.HasValue && !await db.CustomDesigns.AnyAsync(x => x.Id == request.CustomDesignId && x.UserId == UserId, ct)) throw new CommerceException("DESIGN_NOT_FOUND", "Design not found.", 404);
        var row = new CustomerRequest { UserId = UserId, Kind = kind, Subject = request.Subject.Trim(), Details = request.Details.Trim(), CustomDesignId = kind == CustomerRequestKind.CustomQuote ? request.CustomDesignId : null, Quantity = kind == CustomerRequestKind.CustomQuote ? request.Quantity : null };
        db.CustomerRequests.Add(row); await db.SaveChangesAsync(ct);
        await audit.RecordAsync("CustomerRequestCreated", nameof(CustomerRequest), row.Id.ToString(), null, new { Kind = kind.ToString() }, actor.Audit, ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        return Dto(row);
    }
    public async Task Cancel(Guid id, CancellationToken ct)
    {
        await using var transaction = await db.BeginCommerceTransactionAsync(ct);
        var row = await db.CustomerRequests.SingleOrDefaultAsync(x => x.Id == id && x.UserId == UserId, ct) ?? throw Missing();
        if (row.Status is CustomerRequestStatus.Closed or CustomerRequestStatus.Cancelled) throw new CommerceException("REQUEST_CLOSED", "This request is already closed.", 409);
        row.Status = CustomerRequestStatus.Cancelled; row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.RecordAsync("CustomerRequestCancelled", nameof(CustomerRequest), row.Id.ToString(), null, new { Status = "Cancelled" }, actor.Audit, ct);
        if (transaction != null) await transaction.CommitAsync(ct);
    }
    public async Task<object> ReviewQueue(CustomerRequestKind kind, int page, CancellationToken ct)
    {
        await Allowed(kind);
        page = Math.Clamp(page, 1, 100000);
        var query = db.CustomerRequests.AsNoTracking().Where(x => x.Kind == kind);
        var count = await query.CountAsync(ct);
        var rows = await query.OrderBy(x => x.Status).ThenByDescending(x => x.CreatedAt).Skip((page - 1) * 20).Take(20).ToListAsync(ct);
        return new { items = rows.Select(Dto), totalCount = count, page, totalPages = (int)Math.Ceiling(count / 20m) };
    }
    public async Task<object> Review(Guid id, ReviewCustomerRequest request, CancellationToken ct)
    {
        await using var transaction = await db.BeginCommerceTransactionAsync(ct);
        var row = await db.CustomerRequests.SingleOrDefaultAsync(x => x.Id == id, ct) ?? throw Missing();
        await Allowed(row.Kind);
        if (row.Status is CustomerRequestStatus.Closed or CustomerRequestStatus.Cancelled) throw new CommerceException("REQUEST_CLOSED", "This request is closed.", 409);
        if (row.RowVersion.Length > 0 && Convert.ToBase64String(row.RowVersion) != request.RowVersion) throw new CommerceException("REQUEST_CHANGED", "This request changed. Reload before replying.", 409);
        if (!Enum.TryParse<CustomerRequestStatus>(request.Status, out var next) || next is not (CustomerRequestStatus.InReview or CustomerRequestStatus.Responded or CustomerRequestStatus.Closed))
            throw new CommerceException("REQUEST_STATUS_INVALID", "Choose a supported request status.");
        var previous = row.Status;
        if (string.IsNullOrWhiteSpace(request.Reply)) throw new CommerceException("REPLY_REQUIRED", "Add a customer-visible reply.");
        row.Status = next; row.Reply = request.Reply.Trim(); row.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        await audit.RecordAsync("CustomerRequestReviewed", nameof(CustomerRequest), row.Id.ToString(), new { Status = previous.ToString() }, new { Status = next.ToString(), Kind = row.Kind.ToString() }, actor.Audit, ct);
        if (transaction != null) await transaction.CommitAsync(ct);
        return Dto(row);
    }
    private async Task Allowed(CustomerRequestKind kind)
    {
        var policy = kind == CustomerRequestKind.PrivacyDeletion ? AppPolicies.ManageStaff : AppPolicies.ManageOrders;
        if (!(await authorization.AuthorizeAsync(actor.User, policy)).Succeeded) throw new CommerceException("ACCESS_DENIED", "Access denied.", 403);
    }
    private static CommerceException Missing() => new("REQUEST_NOT_FOUND", "Request not found.", 404);
    private static object Dto(CustomerRequest row) => new { row.Id, kind = row.Kind.ToString(), status = row.Status.ToString(), row.Subject, row.Details, row.Quantity, row.CustomDesignId, row.Reply, row.CreatedAt, row.UpdatedAt, rowVersion = Convert.ToBase64String(row.RowVersion) };
}
