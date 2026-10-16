using System.Text.Json;
using be.Application.Abstractions.Payments;
using be.Domain.Common;

namespace be.Infrastructure.Payments.Datatrans;
public sealed class DatatransWebhookParser(DatatransWebhookVerifier verifier) : IPaymentWebhookParser
{
    public string Provider => "Datatrans";

    public GatewayStatus ReadVerified(byte[] body, string signature)
    {
        if (!verifier.Verify(body, signature, DateTimeOffset.UtcNow))
            throw new CommerceException("INVALID_WEBHOOK_SIGNATURE", "Invalid webhook signature.", 401);
        try
        {
            using var json = JsonDocument.Parse(body);
            return DatatransPaymentGateway.ParseStatus(json.RootElement);
        }
        catch (Exception exception)when (exception is JsonException or InvalidOperationException or FormatException)
        {
            throw new CommerceException("WEBHOOK_INVALID", "Invalid webhook payload.");
        }
    }
}
