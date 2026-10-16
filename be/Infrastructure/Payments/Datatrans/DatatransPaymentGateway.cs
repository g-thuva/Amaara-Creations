using be.Domain.Common;
using be.Application.Abstractions.Payments;
using be.Domain.Payments;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Payments.Datatrans;
public sealed class DatatransPaymentGateway(HttpClient client, IOptions<DatatransOptions> options) : IPaymentGateway
{
    public string Provider => "Datatrans";
    public string MerchantId => options.Value.MerchantId;

    public void EnsureAvailable(string currency)
    {
        var o = options.Value;
        if (!o.Enabled || o.Environment != "Sandbox" || o.ApiVersion != "v1" || !Regex.IsMatch(o.MerchantId, "^[0-9]{5,20}$") || string.IsNullOrWhiteSpace(o.ApiPassword))
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Sandbox payment is not configured yet.", 503);
        try
        {
            if (Convert.FromHexString(o.HmacKey).Length < 32)
                throw new FormatException();
        }
        catch (FormatException)
        {
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Sandbox webhook security is not configured yet.", 503);
        }

        if (!o.VerifiedCurrencies.Contains(currency, StringComparer.Ordinal))
            throw new CommerceException("PAYMENT_CURRENCY_UNSUPPORTED", "The sandbox merchant has not been verified for the store currency.", 503);
        if (o.PaymentMethods == null || o.PaymentMethods.Length == 0 || o.PaymentMethods.Any(method => string.IsNullOrWhiteSpace(method) || !Regex.IsMatch(method, "^[A-Z0-9]{3}$")) || o.PaymentMethods.Distinct(StringComparer.Ordinal).Count() != o.PaymentMethods.Length)
            throw new CommerceException("PAYMENT_METHOD_UNVERIFIED", "No verified sandbox payment method is configured.", 503);
        if (!Uri.TryCreate(o.WebhookUrl, UriKind.Absolute, out var hook) || hook.Scheme != "https" || !string.IsNullOrEmpty(hook.UserInfo) || !string.IsNullOrEmpty(hook.Fragment))
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Configure a public HTTPS sandbox webhook.", 503);
        if (!Uri.TryCreate(o.FrontendBaseUrl, UriKind.Absolute, out var frontend) || !string.IsNullOrEmpty(frontend.UserInfo) || !string.IsNullOrEmpty(frontend.Fragment) || !string.IsNullOrEmpty(frontend.Query) || (frontend.Scheme != "https" && !(frontend.Scheme == "http" && frontend.IsLoopback)))
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Configure a valid payment return address.", 503);
    }

    public string RedirectUrl(string transactionId)
    {
        ValidateTransactionId(transactionId);
        return $"https://pay.sandbox.datatrans.com/v1/start/{transactionId}";
    }

    public async Task<GatewayInitialization> InitializeAsync(GatewayPaymentRequest request, CancellationToken ct)
    {
        EnsureAvailable(request.Payment.Currency);
        var o = options.Value;
        var phone = Phone(request.Phone, request.Order.ShippingCountry, o);
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new CommerceException("ADDRESS_INVALID", "A customer email is required for secure payment.");
        string Return(string outcome) => $"{o.FrontendBaseUrl.TrimEnd('/')}/checkout/payment/{outcome}?paymentId={request.Payment.Id}";
        var payload = new JsonObject
        {
            ["amount"] = Money.ToMinor(request.Payment.Amount, request.Payment.Currency),
            ["currency"] = request.Payment.Currency,
            ["refno"] = request.Payment.ProviderReference,
            ["autoSettle"] = o.AutoSettle,
            ["redirect"] = new JsonObject
            {
                ["successUrl"] = Return("return"),
                ["cancelUrl"] = Return("cancel"),
                ["errorUrl"] = Return("error"),
                ["method"] = "GET"
            },
            ["webhook"] = new JsonObject
            {
                ["url"] = o.WebhookUrl
            },
            ["card"] = new JsonObject
            {
                ["3D"] = new JsonObject
                {
                    ["cardholder"] = new JsonObject
                    {
                        ["cardholderName"] = request.Order.ShippingRecipientName,
                        ["email"] = request.Email,
                        ["homePhone"] = new JsonObject
                        {
                            ["cc"] = phone.Code,
                            ["subscriber"] = phone.Subscriber
                        }
                    }
                }
            }
        };
        payload["paymentMethods"] = new JsonArray(o.PaymentMethods.Select(m => (JsonNode? )JsonValue.Create(m)).ToArray());
        using var response = await SendAsync(HttpMethod.Post, "transactions", payload, request.IdempotencyKey, ct);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var id = json.RootElement.GetProperty("transactionId").GetString() ?? throw InvalidResponse();
        return new(id, RedirectUrl(id), id);
    }

    public async Task<GatewayStatus> StatusAsync(string transactionId, CancellationToken ct)
    {
        ValidateTransactionId(transactionId);
        using var response = await SendAsync(HttpMethod.Get, $"transactions/{transactionId}", null, null, ct);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        return ParseStatus(json.RootElement);
    }

    public async Task CancelAsync(Payment payment, Guid idempotencyKey, CancellationToken ct)
    {
        ValidateTransactionId(payment.ProviderTransactionId!);
        using var response = await SendAsync(HttpMethod.Post, $"transactions/{payment.ProviderTransactionId}/cancel", new { refno = payment.ProviderReference }, idempotencyKey, ct);
    }

    public async Task SettleAsync(Payment payment, Guid idempotencyKey, CancellationToken ct)
    {
        ValidateTransactionId(payment.ProviderTransactionId!);
        using var response = await SendAsync(HttpMethod.Post, $"transactions/{payment.ProviderTransactionId}/settle", new { amount = Money.ToMinor(payment.Amount, payment.Currency), currency = payment.Currency, refno = payment.ProviderReference }, idempotencyKey, ct);
    }

    public async Task<string> RefundAsync(Payment payment, Refund refund, CancellationToken ct)
    {
        ValidateTransactionId(payment.ProviderTransactionId!);
        using var response = await SendAsync(HttpMethod.Post, $"transactions/{payment.ProviderTransactionId}/credit", new { amount = Money.ToMinor(refund.Amount, refund.Currency), currency = refund.Currency, refno = refund.ProviderReference }, refund.IdempotencyKey, ct);
        using var json = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var id = json.RootElement.GetProperty("transactionId").GetString() ?? throw InvalidResponse();
        ValidateTransactionId(id);
        return id;
    }

    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? payload, Guid? key, CancellationToken ct)
    {
        var o = options.Value;
        if (o.Environment != "Sandbox" || o.ApiVersion != "v1" || !o.Enabled)
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Sandbox payment is not configured.", 503);
        using var request = new HttpRequestMessage(method, new Uri(new Uri("https://api.sandbox.datatrans.com/v1/"), path));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{o.MerchantId}:{o.ApiPassword}")));
        if (key.HasValue)
            request.Headers.Add("Idempotency-Key", key.Value.ToString());
        if (payload != null)
            request.Content = JsonContent.Create(payload);
        var response = await client.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode)
        {
            var status = (int)response.StatusCode;
            response.Dispose();
            // Provider bodies may contain private data; expose a stable safe error only.
            throw new CommerceException(status is >= 400 and < 500 && status != 409 ? "PAYMENT_PROVIDER_REJECTED" : "PAYMENT_PROVIDER_UNAVAILABLE", "The payment provider could not complete this request.", 502);
        }

        return response;
    }

    public static GatewayState MapStatus(string status) => status switch
    {
        "initialized" or "authenticated" => GatewayState.Pending,
        "authorized" => GatewayState.Authorized,
        "settled" or "transmitted" => GatewayState.Paid,
        "canceled" => GatewayState.Cancelled,
        "failed" => GatewayState.Failed,
        _ => GatewayState.Unknown
    };
    public static GatewayStatus ParseStatus(JsonElement root)
    {
        string Required(string name) => root.TryGetProperty(name, out var p) && p.ValueKind == JsonValueKind.String ? p.GetString()! : throw InvalidResponse();
        long? DetailAmount(string operation) => root.TryGetProperty("detail", out var detail) && detail.TryGetProperty(operation, out var op) && op.TryGetProperty("amount", out var amount) && amount.TryGetInt64(out var result) ? result : null;
        var status = Required("status");
        var type = Required("type");
        var amount = type == "credit" ? DetailAmount("credit") : DetailAmount("authorize") ?? DetailAmount("init");
        var settled = DetailAmount("settle");
        // v1 status responses can report settlement in history instead of detail.settle.
        if (root.TryGetProperty("history", out var history) && history.ValueKind == JsonValueKind.Array)
        {
            long HistoryAmount(string action) => history.EnumerateArray()
                .Where(h => h.TryGetProperty("action", out var a) && a.GetString() == action && h.TryGetProperty("success", out var success) && success.ValueKind == JsonValueKind.True && h.TryGetProperty("amount", out var v) && v.TryGetInt64(out _))
                .Sum(h => h.GetProperty("amount").GetInt64());
            if (!settled.HasValue && history.EnumerateArray().Any(h => h.TryGetProperty("action", out var a) && a.GetString() == "settle"))
                settled = HistoryAmount("settle");
            if (!amount.HasValue)
            {
                var historical = HistoryAmount(type == "credit" ? "credit" : "authorize");
                if (historical > 0)
                    amount = historical;
            }
        }

        var id = Required("transactionId");
        ValidateTransactionId(id);
        return new(id, Required("merchantId"), Required("refno"), Required("currency"), amount, settled, type, status, MapStatus(status), root.TryGetProperty("paymentMethod", out var method) ? method.GetString() : null);
    }

    private static (string Code, string Subscriber) Phone(string? value, string? country, DatatransOptions o)
    {
        var digits = Regex.Replace(value ?? string.Empty, "[\\s()\\-]", "");
        if (!o.PhoneCallingCodes.TryGetValue(country ?? string.Empty, out var code) || !Regex.IsMatch(code, "^[1-9][0-9]{0,2}$"))
            throw new CommerceException("ADDRESS_INVALID", "Configure a calling code for this destination before secure payment.");
        if (digits.StartsWith("+" + code, StringComparison.Ordinal))
            digits = digits[(code.Length + 1)..];
        else if (digits.StartsWith('0'))
            digits = digits[1..];
        else
            throw new CommerceException("ADDRESS_INVALID", "Use an international phone number or a local number beginning with zero.");
        if (!Regex.IsMatch(digits, "^[0-9]{5,12}$"))
            throw new CommerceException("ADDRESS_INVALID", "Enter a valid phone number for secure payment.");
        return (code, digits);
    }

    private static void ValidateTransactionId(string id)
    {
        if (string.IsNullOrWhiteSpace(id) || !Regex.IsMatch(id, "^[0-9]{10,30}$"))
            throw InvalidResponse();
    }

    private static CommerceException InvalidResponse() => new("PAYMENT_PROVIDER_RESPONSE_INVALID", "The payment provider response could not be verified.", 502);
}
