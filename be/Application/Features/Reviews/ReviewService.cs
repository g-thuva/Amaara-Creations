using be.Domain.Common;
using be.Application.Abstractions.Audit;
using be.Domain.Orders;
using be.Domain.Reviews;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using be.Application.Abstractions.Persistence;
using be.Application.Abstractions.Identity;

namespace be.Application.Features.Reviews;
public sealed class ReviewService(IApplicationDbContext db, IAuditService audit, IRequestActor _actor)
{
    public async Task<object?> Public(int productId, int page = 1, CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId && p.IsActive, ct))
            throw new CommerceException("REVIEWS_NOT_FOUND", "Resource not found.", 404);
        var query = db.Reviews.AsNoTracking().Where(r => r.ProductId == productId && r.ModerationStatus == "Published");
        var ratings = await query.GroupBy(r => r.Rating).Select(g => new { Rating = g.Key, Count = g.Count() }).ToListAsync(ct);
        var count = ratings.Sum(r => r.Count);
        return new
        {
            reviews = await query.OrderByDescending(r => r.Id)
            .Skip((Math.Max(1, page) - 1) * 30)
            .Take(30)
            .Select(r => new { r.Id, r.ProductId, UserName = r.User!.Name, r.Rating, r.Comment, r.CreatedAt, r.UpdatedAt, VerifiedPurchase = r.OrderItemId != null })
            .ToListAsync(ct),
            totalCount = count,
            averageRating = count == 0 ? 0 : Math.Round(ratings.Sum(r => r.Rating * r.Count) / (double)count, 2),
            ratingDistribution1 = ratings.Where(r => r.Rating == 1).Sum(r => r.Count),
            ratingDistribution2 = ratings.Where(r => r.Rating == 2).Sum(r => r.Count),
            ratingDistribution3 = ratings.Where(r => r.Rating == 3).Sum(r => r.Count),
            ratingDistribution4 = ratings.Where(r => r.Rating == 4).Sum(r => r.Count),
            ratingDistribution5 = ratings.Where(r => r.Rating == 5).Sum(r => r.Count)
        };
    }

    public async Task<object?> Create(int productId, CreateReviewRequest request, CancellationToken ct)
    {
        var userId = _actor.User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await db.Products.AnyAsync(p => p.Id == productId && p.IsActive, ct))
            throw new CommerceException("REVIEWS_NOT_FOUND", "Resource not found.", 404);
        await using var tx = await db.BeginCommerceTransactionAsync(ct);
        var existing = await db.Reviews.SingleOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId, ct);
        if (existing != null && existing.ModerationStatus != "Deleted")
            throw new CommerceException("REVIEWS_CONFLICT", "You have already reviewed this product. Edit your existing review.", 409);
        var itemId = await db.OrderItems.Where(i => i.ProductId == productId && i.Order!.UserId == userId && (i.Order.Status == OrderStatus.Completed || i.Order.Status == OrderStatus.Delivered) && i.Order.Payments.Any(p => p.SettledAmount > p.RefundedAmount))
            .OrderByDescending(i => i.Id)
            .Select(i => (int? )i.Id)
            .FirstOrDefaultAsync(ct);
        var review = existing ?? new Review
        {
            ProductId = productId,
            UserId = userId
        };
        review.Rating = request.Rating;
        review.Comment = request.Comment?.Trim();
        review.OrderItemId = itemId;
        review.ModerationStatus = "Pending";
        review.ModerationReason = null;
        review.UpdatedAt = DateTime.UtcNow;
        if (existing == null)
            db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);
        if (tx != null)
            await tx.CommitAsync(ct);
        return new
        {
            review.Id,
            review.Rating,
            review.Comment,
            review.ModerationStatus,
            VerifiedPurchase = itemId != null
        };
    }

    public async Task<object?> Mine(int? productId, CancellationToken ct) => await db.Reviews.AsNoTracking()
        .Where(r => r.UserId == _actor.User.FindFirstValue(ClaimTypes.NameIdentifier) && r.ModerationStatus != "Deleted" && (!productId.HasValue || r.ProductId == productId))
        .OrderByDescending(r => r.Id)
        .Take(100)
        .Select(r => new { r.Id, r.ProductId, r.Rating, r.Comment, r.ModerationStatus, VerifiedPurchase = r.OrderItemId != null })
        .ToListAsync(ct);
    public async Task<object?> Edit(int id, UpdateReviewRequest request, CancellationToken ct)
    {
        var r = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id && r.UserId == _actor.User.FindFirstValue(ClaimTypes.NameIdentifier), ct);
        if (r == null)
            throw new CommerceException("REVIEWS_NOT_FOUND", "Resource not found.", 404);
        r.Rating = request.Rating;
        r.Comment = request.Comment?.Trim();
        r.ModerationStatus = "Pending";
        r.ModerationReason = null;
        r.UpdatedAt = DateTime.UtcNow;
        await audit.RecordAsync("ReviewEditedByOwner", "Review", id.ToString(), null, new { r.ModerationStatus }, _actor.Audit, ct);
        return new
        {
            r.Id,
            r.Rating,
            r.Comment,
            r.ModerationStatus
        };
    }

    public async Task DeleteOwn(int id, CancellationToken ct)
    {
        var r = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id && r.UserId == _actor.User.FindFirstValue(ClaimTypes.NameIdentifier), ct);
        if (r == null)
            throw new CommerceException("REVIEWS_NOT_FOUND", "Resource not found.", 404);
        r.ModerationStatus = "Deleted";
        r.Comment = null;
        r.UpdatedAt = DateTime.UtcNow;
        await audit.RecordAsync("ReviewDeletedByOwner", "Review", id.ToString(), null, null, _actor.Audit, ct);
        return;
    }

    public async Task<object?> List(
        string? status,
        string? search,
        int? productId,
        int? rating,
        int pageNumber = 1,
        int pageSize = 20,
        CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Reviews.AsNoTracking()
            .Where(r => (status == null || r.ModerationStatus == status) && (!productId.HasValue || r.ProductId == productId) && (!rating.HasValue || r.Rating == rating));
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(r => r.User!.Name.Contains(search) || r.Comment != null && r.Comment.Contains(search));
        return new
        {
            reviews = await query.OrderByDescending(r => r.Id)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(r => new { r.Id, r.ProductId, ProductName = r.Product!.Name, UserName = r.User!.Name, r.Rating, r.Comment, r.ModerationStatus, r.ModerationReason, r.CreatedAt, r.UpdatedAt, VerifiedPurchase = r.OrderItemId != null, r.RowVersion })
            .ToListAsync(ct),
            totalCount = await query.CountAsync(ct),
            pageNumber,
            pageSize
        };
    }

    public async Task Moderate(int id, ModerationRequest request, CancellationToken ct)
    {
        if (request.Status is not ("Published" or "Hidden" or "Deleted") || string.IsNullOrWhiteSpace(request.Reason))
            throw new CommerceException("MODERATION_INVALID", "Choose a moderation state and provide a reason.");
        var r = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (r == null)
            throw new CommerceException("REVIEWS_NOT_FOUND", "Resource not found.", 404);
        if (request.RowVersion != Convert.ToBase64String(r.RowVersion))
            throw new CommerceException("REVIEW_CHANGED", "Refresh this review before moderating.", 409);
        var previous = r.ModerationStatus;
        r.ModerationStatus = request.Status;
        r.ModerationReason = request.Reason.Trim();
        r.UpdatedAt = DateTime.UtcNow;
        await audit.RecordAsync("ReviewModerated", "Review", id.ToString(), new { Status = previous }, new { request.Status, request.Reason }, _actor.Audit, ct);
        return;
    }

    public Task DeleteAdmin(int id, ModerationRequest request, CancellationToken ct) => Moderate(id, request with { Status = "Deleted" }, ct);
    public async Task<object?> Stats(CancellationToken ct) => new
    {
        totalReviews = await db.Reviews.CountAsync(ct),
        pending = await db.Reviews.CountAsync(r => r.ModerationStatus == "Pending", ct),
        averageRating = await db.Reviews.Where(r => r.ModerationStatus == "Published").AverageAsync(r => (double? )r.Rating, ct) ?? 0
    };
}
