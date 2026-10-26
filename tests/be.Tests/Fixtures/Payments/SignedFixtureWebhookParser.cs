using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using be.Application.Abstractions.Payments;
using be.Domain.Common;

namespace be.Tests.Fixtures.Payments;

public sealed class SignedFixtureWebhookParser : IPaymentWebhookParser
{
    private readonly byte[] key = RandomNumberGenerator.GetBytes(32);
    public string Provider => PaymentFixtureSafety.Provider;
    public sealed record Event(Guid EventId, GatewayStatus Status);
    private static readonly JsonSerializerOptions Json = new() { Converters = { new JsonStringEnumConverter() } };
    public byte[] Payload(Guid id, GatewayStatus status) => JsonSerializer.SerializeToUtf8Bytes(new Event(id, status), Json);
    public string Sign(byte[] body) => Convert.ToHexString(HMACSHA256.HashData(key, body));
    public GatewayStatus ReadVerified(byte[] body, string signature)
    {
        byte[] provided;
        try { provided = Convert.FromHexString(signature); }
        catch (FormatException) { throw new CommerceException("FIXTURE_SIGNATURE_INVALID", "Invalid simulated event signature.", 401); }
        if (body.Length > 65536 || !CryptographicOperations.FixedTimeEquals(HMACSHA256.HashData(key, body), provided))
            throw new CommerceException("FIXTURE_SIGNATURE_INVALID", "Invalid simulated event signature.", 401);
        var value = JsonSerializer.Deserialize<Event>(body, Json) ?? throw new CommerceException("FIXTURE_EVENT_INVALID", "Invalid simulated event.");
        if (value.EventId == Guid.Empty || value.Status.Mode != PaymentFixtureSafety.Mode || value.Status.InvoiceTest != true)
            throw new CommerceException("FIXTURE_EVENT_INVALID", "Only explicitly simulated events are accepted.");
        return value.Status;
    }
}
