using System.Text.Json;
using be.Application.Abstractions.Payments;
using be.Domain.Common;

namespace be.Infrastructure.Payments.Zahls;
public sealed class ZahlsWebhookParser(ZahlsWebhookVerifier verifier) : IPaymentWebhookParser
{
    public string Provider => "Zahls";

    public GatewayStatus ReadVerified(byte[] body, string signature)
    {
        if (!verifier.Verify(body, signature))
            throw new CommerceException("INVALID_WEBHOOK_SIGNATURE", "Invalid webhook signature.", 401);
        try
        {
            using var json = JsonDocument.Parse(body);
            return ZahlsPaymentGateway.ParseWebhook(json.RootElement);
        }
        catch (Exception exception)when (exception is JsonException or InvalidOperationException or FormatException or CommerceException { Code: "PAYMENT_PROVIDER_RESPONSE_INVALID" })
        {
            throw new CommerceException("WEBHOOK_INVALID", "Invalid transaction webhook.");
        }
    }
}
