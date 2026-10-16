using System.Security.Cryptography;
using be.Application.Abstractions.Payments;
using be.Domain.Common;

namespace be.Application.Features.Payments;
public sealed class PaymentWebhookService(IEnumerable<IPaymentWebhookParser> parsers, RefundService refunds, PaymentReconciliationService reconciliation)
{
    public async Task ProcessAsync(string provider, byte[] body, string signature, CancellationToken ct)
    {
        var parser = parsers.SingleOrDefault(p => p.Provider == provider) ?? throw new CommerceException("PAYMENT_PROVIDER_UNAVAILABLE", "This payment provider is unavailable for verification.", 503);
        var state = parser.ReadVerified(body, signature);
        if (state.Type == "credit")
            await refunds.ApplyAsync(state, ct, provider: provider);
        else
            await reconciliation.ApplyAsync(state, Convert.ToHexString(SHA256.HashData(body)), ct, provider: provider);
    }
}
