namespace be.Application.Abstractions.Payments;
public interface IPaymentWebhookParser
{
    string Provider { get; }

    GatewayStatus ReadVerified(byte[] body, string signature);
}
