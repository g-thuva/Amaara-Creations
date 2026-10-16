using be.Domain.Common;
using be.Application.Abstractions.Payments;

namespace be.Application.Features.Payments;
public interface IPaymentGatewayRouter
{
    IPaymentGateway ForProvider(string provider);
}

public sealed class PaymentGatewayRouter(IEnumerable<IPaymentGateway> gateways) : IPaymentGatewayRouter
{
    public IPaymentGateway ForProvider(string provider)
    {
        var matches = gateways.Where(g => string.Equals(g.Provider, provider, StringComparison.OrdinalIgnoreCase)).Take(2).ToArray();
        if (matches.Length != 1)
            throw new CommerceException("PAYMENT_PROVIDER_UNAVAILABLE", "This payment provider is unavailable for verification.", 503);
        return matches[0];
    }
}
