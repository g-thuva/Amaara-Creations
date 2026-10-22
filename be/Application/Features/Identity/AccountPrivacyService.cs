using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using be.Application.Abstractions.Audit;
using be.Application.Abstractions.Email;
using be.Application.Abstractions.Identity;
using be.Application.Abstractions.Persistence;
using be.Domain.Common;
using be.Domain.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace be.Application.Features.Identity;
public class EmailChangeRequest : SecondFactorRequest
{
    [Required, EmailAddress, StringLength(254)] public string NewEmail { get; set; } = string.Empty;
}
public sealed class ConfirmEmailChangeRequest : EmailChangeRequest
{
    [Required, StringLength(4096)] public string Token { get; set; } = string.Empty;
}
public sealed class DeletionReviewRequest : SecondFactorRequest
{
    [Required, StringLength(2000)] public string Details { get; set; } = string.Empty;
    public bool Acknowledged { get; set; }
}

public sealed class AccountPrivacyService(AccountReauthentication reauthentication, UserManager<User> users, IRefreshSessionService sessions,
    IApplicationDbContext db, IEmailService email, IConfiguration configuration, IAuditService audit, IRequestActor actor, CustomerRequestService requests)
{
    public async Task RequestEmailChange(EmailChangeRequest request, CancellationToken ct)
    {
        Enabled();
        var user = await reauthentication.Verify(request, ct);
        var target = request.NewEmail.Trim();
        // The response does not reveal whether another account uses this address.
        if (await users.FindByEmailAsync(target) != null) return;
        var token = await users.GenerateChangeEmailTokenAsync(user, target);
        // No token or email is placed in a browser URL or client log.
        await email.SendTransactionAsync(new User { Email = target, Name = user.Name }, "Confirm your sign-in email change", $"Paste this verification token into Account security to confirm your email change. It expires in 30 minutes.\n\n{token}\n\nIf you did not request this change, ignore this message.", ct);
        await email.SendTransactionAsync(user, "An email change was requested", "An email change was requested for your account. Your sign-in email remains unchanged until verification succeeds. If this was not you, change your password and sign out all devices in Account security.", ct);
        await audit.RecordAsync("AccountEmailChangeRequested", nameof(User), user.Id, null, new { Requested = true }, actor.Audit, ct);
    }
    public async Task ConfirmEmailChange(ConfirmEmailChangeRequest request, CancellationToken ct)
    {
        Enabled();
        var user = await reauthentication.Verify(request, ct);
        await using (var transaction = await db.BeginCommerceTransactionAsync(ct))
        {
            var target = request.NewEmail.Trim();
            var result = await users.ChangeEmailAsync(user, target, request.Token);
            if (!result.Succeeded) throw new CommerceException("EMAIL_CHANGE_INVALID", "The verification token is invalid, expired or unavailable. Request a new token.");
            result = await users.SetUserNameAsync(user, target);
            if (!result.Succeeded) throw new CommerceException("EMAIL_CHANGE_INVALID", "This email change could not be completed.");
            await audit.RecordAsync("AccountEmailChanged", nameof(User), user.Id, null, new { Changed = true }, actor.Audit, ct);
            if (transaction != null) await transaction.CommitAsync(ct);
        }
        await sessions.RevokeAllForUserAsync(user.Id, ct);
    }
    public async Task<object> Export(SecondFactorRequest request, CancellationToken ct)
    {
        var user = await reauthentication.Verify(request, ct);
        var id = user.Id;
        // Bounded synchronous exports fail explicitly rather than silently omitting
        // records. Large histories can use the reviewed operator export process.
        var counts = new[] { await db.Orders.CountAsync(x => x.UserId == id, ct), await db.OrderItems.CountAsync(x => x.Order!.UserId == id, ct), await db.CustomDesigns.CountAsync(x => x.UserId == id, ct), await db.CustomDesignAssets.CountAsync(x => x.CustomDesign!.UserId == id, ct), await db.Reviews.CountAsync(x => x.UserId == id, ct) };
        if (counts.Any(count => count > 10000)) throw new CommerceException("EXPORT_REVIEW_REQUIRED", "This account has a large history. Request an assisted export through the approved support process.", 409);
        var addresses = await db.Addresses.AsNoTracking().Where(x => x.UserId == id).Select(x => new { x.Label, x.RecipientName, x.Phone, x.AddressLine1, x.AddressLine2, x.City, x.DistrictOrProvince, x.PostalCode, x.Country, x.IsDefault }).Take(10001).ToListAsync(ct);
        var orders = await db.Orders.AsNoTracking().Where(x => x.UserId == id).OrderBy(x => x.Id).Take(10001).Select(x => new { x.OrderNumber, x.OrderDate, x.Status, x.Currency, x.Subtotal, x.DiscountTotal, x.ShippingTotal, x.TaxTotal, x.PaymentFee, x.Total, x.ShippingAddress, x.ShippingCity, x.ShippingPostalCode, x.ShippingCountry, items = x.OrderItems.Select(i => new { i.ProductNameSnapshot, i.SkuSnapshot, i.Quantity, i.UnitPrice, i.Subtotal, i.ConfigurationSnapshotJson }) }).ToListAsync(ct);
        var designs = await db.CustomDesigns.AsNoTracking().Where(x => x.UserId == id).OrderBy(x => x.Id).Take(10001).Select(x => new { x.Id, x.Name, x.Status, x.Width, x.Height, x.Quantity, x.ShapeCode, x.MaterialCode, x.FinishCode, x.FontCode, x.ColourCode, x.CustomText, x.TextAlignment, editorStateJson = x.DesignJson, x.CreatedAt, x.UpdatedAt, artwork = x.Assets.Select(a => new { a.Id, a.OriginalFileName, a.ContentType, a.Width, a.Height }) }).ToListAsync(ct);
        var wishlist = await db.WishlistItems.AsNoTracking().Where(x => x.UserId == id).Select(x => new { x.ProductId, x.CreatedAt }).Take(10001).ToListAsync(ct);
        var reviews = await db.Reviews.AsNoTracking().Where(x => x.UserId == id).OrderBy(x => x.Id).Take(10001).Select(x => new { x.ProductId, x.Rating, x.Comment, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
        var accountRequests = await db.CustomerRequests.AsNoTracking().Where(x => x.UserId == id).Take(10001).Select(x => new { x.Id, x.Kind, x.Status, x.Subject, x.Details, x.Reply, x.CreatedAt, x.UpdatedAt }).ToListAsync(ct);
        if (addresses.Count > 10000 || orders.Count > 10000 || designs.Count > 10000 || wishlist.Count > 10000 || reviews.Count > 10000 || accountRequests.Count > 10000) throw new CommerceException("EXPORT_REVIEW_REQUIRED", "Request an assisted export for this large account history.", 409);
        await audit.RecordAsync("AccountDataExported", nameof(User), id, null, new { Exported = true }, actor.Audit, ct);
        return new { exportVersion = 1, exportedAt = DateTime.UtcNow, profile = new { user.Name, user.Email, user.Phone, user.Address, user.AvatarUrl, user.EmailConfirmed, user.CreatedAt, user.UpdatedAt }, addresses, orders, designs, wishlist, reviews, requests = accountRequests, note = "Artwork and proof files remain available through your authenticated saved-design and order pages. This export does not contain passwords, security tokens or internal staff notes." };
    }
    public async Task<object> DeletionReview(DeletionReviewRequest request, CancellationToken ct)
    {
        if (!configuration.GetValue<bool>("Privacy:RequestsEnabled")) throw new CommerceException("REQUESTS_DISABLED", "This online request service is not enabled.", 503);
        if (!request.Acknowledged) throw new CommerceException("REQUEST_ACKNOWLEDGMENT_REQUIRED", "Confirm that this submits a review request, rather than deleting data immediately.");
        await reauthentication.Verify(request, ct);
        return await requests.Create(CustomerRequestKind.PrivacyDeletion, new CreateCustomerRequest { Subject = "Account deletion review", Details = request.Details }, ct);
    }
    private void Enabled() { if (!configuration.GetValue<bool>("Authentication:EmailChangeEnabled")) throw new CommerceException("EMAIL_CHANGE_DISABLED", "Verified email changes are not enabled.", 503); }
}
