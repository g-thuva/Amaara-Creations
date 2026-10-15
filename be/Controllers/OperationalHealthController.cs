using be.Data;
using be.Models;
using be.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace be.Controllers;

[ApiController, Route("api/v1/admin/operational-health"), Authorize(Policy = AppPolicies.ViewReports)]
public sealed class OperationalHealthController(ApplicationDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return Ok(new {
            checkedAt = now,
            outboxUnsent = await db.CommerceNotifications.CountAsync(n => n.SentAt == null, ct),
            outboxExhausted = await db.CommerceNotifications.CountAsync(n => n.SentAt == null && n.Attempts >= 5, ct),
            paymentReviewOrders = await db.Orders.CountAsync(o => o.Status == OrderStatus.PaymentReview, ct),
            pendingOlderThanOneHour = await db.Payments.CountAsync(p => p.Status == PaymentStatus.Pending && p.CreatedAt < now.AddHours(-1), ct),
            refundsPending = await db.Refunds.CountAsync(r => r.Status == "Pending", ct),
            negativeBaseStock = await db.Products.CountAsync(p => p.Stock < 0, ct),
            negativeVariantStock = await db.ProductVariants.CountAsync(v => v.StockQuantity < 0, ct),
            productionOlderThanSevenDays = await db.CustomDesigns.CountAsync(d => d.Status == CustomDesignStatus.Ordered && d.ProductionStatus != ProductionStatus.Ready && d.UpdatedAt < now.AddDays(-7), ct)
        });
    }
}
