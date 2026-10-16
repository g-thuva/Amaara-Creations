using be.Domain.Common;
using be.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using be.Application.Abstractions.Persistence;

namespace be.Application.Features.Payments;
public sealed class PaymentQueries(IApplicationDbContext db)
{
    public async Task<object?> Payments(string? status, string? mode, int page = 1, CancellationToken ct = default)
    {
        var query = db.Payments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status))
        {
            if (!Enum.TryParse<PaymentStatus>(status, out var state) || !Enum.IsDefined(state))
                throw new CommerceException("PAYMENT_STATUS_INVALID", "Choose a valid payment state.");
            query = query.Where(p => p.Status == state);
        }

        if (!string.IsNullOrWhiteSpace(mode))
            query = query.Where(p => p.ProviderMode == mode);
        return new
        {
            items = await query.OrderByDescending(p => p.Id)
            .Skip((Math.Max(1, page) - 1) * 30)
            .Take(30)
            .Select(p => new { p.Id, p.OrderId, p.Order!.OrderNumber, p.Provider, p.ProviderMode, p.Amount, p.Currency, Status = p.Status.ToString(), p.ProviderReference, p.ProviderGatewayId, p.CreatedAt, p.PaidAt, p.LastReconciledAt })
            .ToListAsync(ct),
            totalCount = await query.CountAsync(ct)
        };
    }
}
