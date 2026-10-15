using be.Data;
using be.DTOs.Review;
using be.Models;
using be.Security;
using be.Services;
using be.Services.Checkout;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace be.Controllers;

[ApiController, Route("api/v1/reviews")]
public sealed class ReviewsController(ApplicationDbContext db, IAuditService audit) : ControllerBase
{
    [HttpGet("/api/v1/products/{productId:int}/reviews")]
    public async Task<IActionResult> Public(int productId, int page = 1, CancellationToken ct = default)
    {
        if (!await db.Products.AnyAsync(p => p.Id == productId && p.IsActive, ct)) return NotFound();
        var query = db.Reviews.AsNoTracking().Where(r => r.ProductId == productId && r.ModerationStatus == "Published");
        var ratings = await query.GroupBy(r => r.Rating).Select(g => new { Rating = g.Key, Count = g.Count() }).ToListAsync(ct);
        var count = ratings.Sum(r => r.Count);
        return Ok(new {
            reviews = await query.OrderByDescending(r => r.Id).Skip((Math.Max(1, page) - 1) * 30).Take(30)
                .Select(r => new { r.Id, r.ProductId, UserName = r.User!.Name, r.Rating, r.Comment, r.CreatedAt, r.UpdatedAt, VerifiedPurchase = r.OrderItemId != null }).ToListAsync(ct),
            totalCount = count, averageRating = count == 0 ? 0 : Math.Round(ratings.Sum(r => r.Rating * r.Count) / (double)count, 2),
            ratingDistribution1 = ratings.Where(r => r.Rating == 1).Sum(r => r.Count), ratingDistribution2 = ratings.Where(r => r.Rating == 2).Sum(r => r.Count),
            ratingDistribution3 = ratings.Where(r => r.Rating == 3).Sum(r => r.Count), ratingDistribution4 = ratings.Where(r => r.Rating == 4).Sum(r => r.Count), ratingDistribution5 = ratings.Where(r => r.Rating == 5).Sum(r => r.Count)
        });
    }

    [HttpPost("/api/v1/products/{productId:int}/reviews"), Authorize]
    public async Task<IActionResult> Create(int productId, CreateReviewRequest request, CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
        if (!await db.Products.AnyAsync(p => p.Id == productId && p.IsActive, ct)) return NotFound();
        await using var tx = await CommerceTransaction.BeginAsync(db, ct);
        var existing = await db.Reviews.SingleOrDefaultAsync(r => r.UserId == userId && r.ProductId == productId, ct);
        if (existing != null && existing.ModerationStatus != "Deleted") return Conflict(new { message = "You have already reviewed this product. Edit your existing review." });
        var itemId = await db.OrderItems.Where(i => i.ProductId == productId && i.Order!.UserId == userId
            && (i.Order.Status == OrderStatus.Completed || i.Order.Status == OrderStatus.Delivered)
            && i.Order.Payments.Any(p => p.SettledAmount > p.RefundedAmount))
            .OrderByDescending(i => i.Id).Select(i => (int?)i.Id).FirstOrDefaultAsync(ct);
        var review = existing ?? new Review { ProductId = productId, UserId = userId };
        review.Rating = request.Rating; review.Comment = request.Comment?.Trim(); review.OrderItemId = itemId; review.ModerationStatus = "Pending"; review.ModerationReason = null; review.UpdatedAt = DateTime.UtcNow;
        if (existing == null) db.Reviews.Add(review);
        await db.SaveChangesAsync(ct);
        if (tx != null) await tx.CommitAsync(ct);
        return StatusCode(201, new { review.Id, review.Rating, review.Comment, review.ModerationStatus, VerifiedPurchase = itemId != null });
    }

    [HttpGet("mine"), Authorize]
    public async Task<IActionResult> Mine(int? productId, CancellationToken ct) => Ok(await db.Reviews.AsNoTracking().Where(r => r.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier) && r.ModerationStatus != "Deleted" && (!productId.HasValue || r.ProductId == productId))
        .OrderByDescending(r => r.Id).Take(100).Select(r => new { r.Id, r.ProductId, r.Rating, r.Comment, r.ModerationStatus, VerifiedPurchase = r.OrderItemId != null }).ToListAsync(ct));

    [HttpPut("{id:int}"), Authorize]
    public async Task<IActionResult> Edit(int id, UpdateReviewRequest request, CancellationToken ct)
    {
        var r = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id && r.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier), ct);
        if (r == null) return NotFound();
        r.Rating = request.Rating; r.Comment = request.Comment?.Trim(); r.ModerationStatus = "Pending"; r.ModerationReason = null; r.UpdatedAt = DateTime.UtcNow;
        await audit.RecordAsync("ReviewEditedByOwner", "Review", id.ToString(), null, new { r.ModerationStatus }, HttpContext, ct);
        return Ok(new { r.Id, r.Rating, r.Comment, r.ModerationStatus });
    }

    [HttpDelete("{id:int}"), Authorize]
    public async Task<IActionResult> DeleteOwn(int id, CancellationToken ct)
    {
        var r = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id && r.UserId == User.FindFirstValue(ClaimTypes.NameIdentifier), ct);
        if (r == null) return NotFound();
        r.ModerationStatus = "Deleted"; r.Comment = null; r.UpdatedAt = DateTime.UtcNow;
        await audit.RecordAsync("ReviewDeletedByOwner", "Review", id.ToString(), null, null, HttpContext, ct);
        return NoContent();
    }

    [HttpGet("/api/v1/admin/reviews"), Authorize(Policy = AppPolicies.ManageReviews)]
    public async Task<IActionResult> List(string? status, string? search, int? productId, int? rating, int pageNumber = 1, int pageSize = 20, CancellationToken ct = default)
    {
        pageNumber = Math.Max(1, pageNumber); pageSize = Math.Clamp(pageSize, 1, 100);
        var query = db.Reviews.AsNoTracking().Where(r => (status == null || r.ModerationStatus == status) && (!productId.HasValue || r.ProductId == productId) && (!rating.HasValue || r.Rating == rating));
        if (!string.IsNullOrWhiteSpace(search)) query = query.Where(r => r.User!.Name.Contains(search) || r.Comment != null && r.Comment.Contains(search));
        return Ok(new { reviews = await query.OrderByDescending(r => r.Id).Skip((pageNumber - 1) * pageSize).Take(pageSize)
            .Select(r => new { r.Id, r.ProductId, ProductName = r.Product!.Name, UserName = r.User!.Name, r.Rating, r.Comment, r.ModerationStatus, r.ModerationReason, r.CreatedAt, r.UpdatedAt, VerifiedPurchase = r.OrderItemId != null, r.RowVersion }).ToListAsync(ct), totalCount = await query.CountAsync(ct), pageNumber, pageSize });
    }

    public sealed record ModerationRequest(string Status, [Required, StringLength(500)] string Reason, [Required(AllowEmptyStrings = true)] string RowVersion);
    [HttpPut("/api/v1/admin/reviews/{id:int}/moderation"), Authorize(Policy = AppPolicies.ManageReviews)]
    public async Task<IActionResult> Moderate(int id, ModerationRequest request, CancellationToken ct)
    {
        if (request.Status is not ("Published" or "Hidden" or "Deleted") || string.IsNullOrWhiteSpace(request.Reason)) throw new CommerceException("MODERATION_INVALID", "Choose a moderation state and provide a reason.");
        var r = await db.Reviews.SingleOrDefaultAsync(r => r.Id == id, ct);
        if (r == null) return NotFound();
        if (request.RowVersion != Convert.ToBase64String(r.RowVersion)) throw new CommerceException("REVIEW_CHANGED", "Refresh this review before moderating.", 409);
        var previous = r.ModerationStatus; r.ModerationStatus = request.Status; r.ModerationReason = request.Reason.Trim(); r.UpdatedAt = DateTime.UtcNow;
        await audit.RecordAsync("ReviewModerated", "Review", id.ToString(), new { Status = previous }, new { request.Status, request.Reason }, HttpContext, ct);
        return NoContent();
    }

    [HttpDelete("/api/v1/admin/reviews/{id:int}"), Authorize(Policy = AppPolicies.ManageReviews)]
    public Task<IActionResult> DeleteAdmin(int id, [FromBody] ModerationRequest request, CancellationToken ct) => Moderate(id, request with { Status = "Deleted" }, ct);

    [HttpGet("/api/v1/admin/reviews/stats"), Authorize(Policy = AppPolicies.ManageReviews)]
    public async Task<IActionResult> Stats(CancellationToken ct) => Ok(new { totalReviews = await db.Reviews.CountAsync(ct), pending = await db.Reviews.CountAsync(r => r.ModerationStatus == "Pending", ct), averageRating = await db.Reviews.Where(r => r.ModerationStatus == "Published").AverageAsync(r => (double?)r.Rating, ct) ?? 0 });
}
