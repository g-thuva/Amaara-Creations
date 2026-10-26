using be.Domain.Common;
using be.Application.Abstractions.Payments;
using be.Application.Features.Payments;
using be.Domain.Payments;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Options;

namespace be.Infrastructure.Payments.Zahls;
/// <summary>
/// Hosted Gateway integration. The v1.16 path is shown by the current zahls.ch site;
/// the envelope and authentication follow its official SDKs and require merchant TEST validation.
/// </summary>
public sealed class ZahlsPaymentGateway(HttpClient client, IOptions<ZahlsOptions> options) : IPaymentGateway
{
    private static readonly Regex InstancePattern = new("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex MethodPattern = new("^[a-z0-9][a-z0-9_-]{1,39}$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
    private static readonly Regex VersionPattern = new("^v[1-9][0-9]*\\.[0-9]+$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex IdPattern = new("^[1-9][0-9]{0,19}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    public string Provider => "Zahls";
    public string MerchantId => options.Value.InstanceName;
    public string Mode => options.Value.Mode;

    public void EnsureAvailable(string currency)
        => ValidateConfiguration(options.Value, currency);

    public static void ValidateConfiguration(ZahlsOptions o, string currency)
    {
        if (!o.Enabled)
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Zahls initiation is disabled.", 503);
        if (o.Mode != "TEST")
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Only Zahls TEST mode is supported; LIVE is disabled.", 503);
        if (o.Currency != "CHF" || currency != "CHF")
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "The order currency is unsupported by this Zahls integration.", 503);
        if (!o.TestProviderConfigured)
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "The merchant's genuine Zahls TEST provider has not been verified.", 503);
        if (!o.ChfSupportedByTestProvider)
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "CHF support has not been verified for the merchant's TEST provider.", 503);
        if (!InstancePattern.IsMatch(o.InstanceName) || string.IsNullOrWhiteSpace(o.ApiKey) || string.IsNullOrWhiteSpace(o.WebhookSigningKey) || o.TimeoutSeconds is < 2 or > 60)
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Zahls TEST credentials or security settings are incomplete.", 503);
        if (!Uri.TryCreate(o.ApiBaseUrl, UriKind.Absolute, out var api) || api.Scheme != Uri.UriSchemeHttps || api.Host != "api.zahls.ch" || api.Port != 443 || api.AbsolutePath != "/" || !string.IsNullOrEmpty(api.UserInfo) || !string.IsNullOrEmpty(api.Query) || !string.IsNullOrEmpty(api.Fragment) || !VersionPattern.IsMatch(o.ApiVersion))
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Zahls TEST API settings are invalid.", 503);
        // pm is an optional restriction, not evidence that a TEST provider is ready.
        // An empty filter delegates method selection to the verified merchant setup.
        if (o.PaymentMethods is null || o.PaymentMethods.Any(m => m is null || !MethodPattern.IsMatch(m)) || o.PaymentMethods.Distinct(StringComparer.OrdinalIgnoreCase).Count() != o.PaymentMethods.Length)
            throw new CommerceException("PAYMENT_METHOD_UNVERIFIED", "The optional Zahls payment-method filter is invalid.", 503);
        if (!Uri.TryCreate(o.WebhookUrl, UriKind.Absolute, out var hook) || hook.Scheme != Uri.UriSchemeHttps || hook.IsLoopback || hook.Port != 443 || hook.AbsolutePath != "/api/v1/payments/zahls/webhook" || !string.IsNullOrEmpty(hook.Query) || !string.IsNullOrEmpty(hook.UserInfo) || !string.IsNullOrEmpty(hook.Fragment))
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Configure a public HTTPS Zahls TEST webhook.", 503);
        if (!Uri.TryCreate(o.FrontendBaseUrl, UriKind.Absolute, out var frontend) || !string.IsNullOrEmpty(frontend.UserInfo) || !string.IsNullOrEmpty(frontend.Query) || !string.IsNullOrEmpty(frontend.Fragment) || (frontend.Scheme != Uri.UriSchemeHttps && !(frontend.Scheme == Uri.UriSchemeHttp && frontend.IsLoopback)))
            throw new CommerceException("PAYMENT_NOT_CONFIGURED", "Configure a valid payment return address.", 503);
    }

    public string RedirectUrl(string gatewayId) => throw new CommerceException("PAYMENT_REDIRECT_UNAVAILABLE", "The stored hosted payment link is required to resume this payment.", 409);
    public async Task<GatewayInitialization> InitializeAsync(GatewayPaymentRequest request, CancellationToken ct)
    {
        EnsureAvailable(request.Payment.Currency);
        if (request.Order.Currency != "CHF" || request.Payment.Amount != request.Order.Total || string.IsNullOrWhiteSpace(request.Payment.ProviderReference) || string.IsNullOrWhiteSpace(request.Email))
            throw new CommerceException("PAYMENT_AMOUNT_MISMATCH", "Checkout payment details do not match the saved order.", 409);
        var amount = Minor(request.Payment.Amount, "CHF");
        var o = options.Value;
        string Return(string outcome) => $"{o.FrontendBaseUrl.TrimEnd('/')}/checkout/payment/{outcome}?paymentId={request.Payment.Id}";
        var fields = new Dictionary<string, object>
        {
            ["email"] = new
            {
                value = request.Email
            }
        };
        if (!string.IsNullOrWhiteSpace(request.Phone))
            fields["phone"] = new
            {
                value = request.Phone
            };
        // Basket is optional in the hosted API. Only send it when every positive line
        // can be represented and its sum exactly equals the charged total. Promotions,
        // excluded tax and payment fees otherwise make a line-only basket misleading.
        var basket = new List<object>();
        var basketTotal = 0m;
        var completeBasket = true;
        foreach (var item in request.Order.OrderItems)
        {
            if (item.Quantity <= 0 || item.UnitPrice <= 0 || string.IsNullOrWhiteSpace(item.ProductNameSnapshot))
            {
                completeBasket = false;
                break;
            }

            var itemAmount = Minor(item.UnitPrice, "CHF");
            basket.Add(new { name = item.ProductNameSnapshot, quantity = item.Quantity, amount = itemAmount });
            basketTotal += itemAmount * item.Quantity;
        }

        if (request.Order.ShippingTotal > 0)
        {
            var shippingAmount = Minor(request.Order.ShippingTotal, "CHF");
            basket.Add(new { name = "Shipping", quantity = 1, amount = shippingAmount });
            basketTotal += shippingAmount;
        }

        if (!completeBasket || basketTotal != amount)
            basket.Clear();
        var payload = new Dictionary<string, object>
        {
            ["amount"] = amount,
            ["currency"] = "CHF",
            ["referenceId"] = request.Payment.ProviderReference,
            ["purpose"] = request.Order.OrderNumber,
            ["successRedirectUrl"] = Return("return"),
            ["failedRedirectUrl"] = Return("error"),
            ["cancelRedirectUrl"] = Return("cancel"),
            ["fields"] = fields
        };
        if (o.PaymentMethods.Length > 0)
            payload["pm"] = o.PaymentMethods;
        if (basket.Count > 0)
            payload["basket"] = basket;
        if (!string.IsNullOrWhiteSpace(o.LookAndFeelProfile))
            payload["lookAndFeelProfile"] = o.LookAndFeelProfile;
        using var json = await SendAsync(HttpMethod.Post, "Gateway/", payload, ct);
        var gateway = SingleData(json.RootElement);
        var id = RequiredId(gateway, "id");
        var link = RequiredString(gateway, "link");
        if (gateway.TryGetProperty("amount", out var returnedAmount) && ReadAmount(returnedAmount) != amount || gateway.TryGetProperty("currency", out var returnedCurrency) && returnedCurrency.GetString() != "CHF" || gateway.TryGetProperty("referenceId", out var returnedReference) && returnedReference.GetString() != request.Payment.ProviderReference)
            throw InvalidResponse();
        link = HostedPaymentUrlPolicy.Validate(link, Provider);
        var linkHost = new Uri(link).IdnHost;
        if (!linkHost.Equals("pay.zahls.ch", StringComparison.OrdinalIgnoreCase) && !linkHost.Equals($"{o.InstanceName}.zahls.ch", StringComparison.OrdinalIgnoreCase))
            throw new CommerceException("PAYMENT_REDIRECT_INVALID", "The secure payment destination could not be verified.", 502);
        return new GatewayInitialization(id, link);
    }

    public Task<GatewayStatus> StatusAsync(string id, CancellationToken ct) => StatusAsync(id, GatewayLookupKind.Transaction, ct);
    public async Task<GatewayStatus> StatusAsync(string id, GatewayLookupKind kind, CancellationToken ct)
    {
        EnsureAvailable("CHF");
        ValidateId(id);
        if (kind == GatewayLookupKind.Transaction)
        {
            using var transactionJson = await SendAsync(HttpMethod.Get, $"Transaction/{id}/", null, ct);
            var state = ParseStatus(SingleData(transactionJson.RootElement));
            if (state.TransactionId != id)
                throw InvalidResponse();
            return state;
        }

        using var gatewayJson = await SendAsync(HttpMethod.Get, $"Gateway/{id}/", null, ct);
        var gateway = SingleData(gatewayJson.RootElement);
        if (RequiredId(gateway, "id") != id)
            throw InvalidResponse();
        var reference = RequiredString(gateway, "referenceId");
        var amount = ReadAmount(Required(gateway, "amount"));
        var currency = RequiredString(gateway, "currency");
        var providerStatus = RequiredString(gateway, "status");
        var transactionId = SingleGatewayTransactionId(gateway);
        if (transactionId is not null)
        {
            var transaction = await StatusAsync(transactionId, GatewayLookupKind.Transaction, ct);
            if (transaction.GatewayId is not null && transaction.GatewayId != id || transaction.Reference != reference || transaction.Amount != amount || transaction.Currency != currency)
                throw InvalidResponse();
            return transaction with
            {
                GatewayId = id
            };
        }

        // A Gateway can exist before a transaction. Its return/status alone is not proof of payment.
        return new GatewayStatus("", options.Value.InstanceName, reference, currency, amount, null, "payment", providerStatus, providerStatus == "waiting" ? GatewayState.Pending : GatewayState.Unknown, null)
        {
            GatewayId = id
        };
    }

    public Task CancelAsync(Payment payment, Guid idempotencyKey, CancellationToken ct) => throw new CommerceException("PAYMENT_OPERATION_UNSUPPORTED", "This Zahls TEST payment does not use deferred authorization.", 409);
    public Task SettleAsync(Payment payment, Guid idempotencyKey, CancellationToken ct) => throw new CommerceException("PAYMENT_OPERATION_UNSUPPORTED", "This Zahls TEST payment does not use deferred capture.", 409);
    public async Task<string> RefundAsync(Payment payment, Refund refund, CancellationToken ct)
    {
        EnsureAvailable(payment.Currency);
        if (payment.Provider != Provider || payment.ProviderMode != "TEST" || refund.Currency != "CHF" || payment.ProviderTransactionId is null || refund.Amount <= 0 || refund.Amount > payment.SettledAmount - payment.RefundedAmount)
            throw new CommerceException("REFUND_VERIFICATION_FAILED", "The payment is not eligible for a Zahls TEST refund.", 409);
        ValidateId(payment.ProviderTransactionId);
        var minor = Minor(refund.Amount, "CHF");
        using var originalJson = await SendAsync(HttpMethod.Get, $"Transaction/{payment.ProviderTransactionId}/", null, ct);
        var originalTransaction = SingleData(originalJson.RootElement);
        var original = ParseStatus(originalTransaction);
        if (original.TransactionId != payment.ProviderTransactionId || original.Type != "payment" || original.ProviderStatus is not ("confirmed" or "partially-refunded") || original.Mode != "TEST" || original.InvoiceTest != true || original.MerchantId != options.Value.InstanceName || original.Reference != payment.ProviderReference || original.Currency != "CHF" || original.Amount != Minor(payment.Amount, "CHF"))
            throw new CommerceException("REFUND_VERIFICATION_FAILED", "The original payment could not be verified for refund.", 409);
        var invoice = Required(originalTransaction, "invoice");
        var providerRefunded = ReadAmount(Required(invoice, "refundedAmount"));
        if (providerRefunded != Money.ToMinor(payment.RefundedAmount, "CHF") || (refund.Amount < payment.SettledAmount - payment.RefundedAmount ? !ReadBoolean(Required(originalTransaction, "partiallyRefundable")) : !ReadBoolean(Required(originalTransaction, "refundable"))))
            throw new CommerceException("REFUND_VERIFICATION_FAILED", "The provider refund balance could not be verified.", 409);
        using var json = await SendAsync(HttpMethod.Post, $"Transaction/{payment.ProviderTransactionId}/refund/", new { amount = minor }, ct);
        var response = SingleData(json.RootElement);
        return RequiredId(response, "id");
    }

    public static GatewayStatus ParseWebhook(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || !root.TryGetProperty("transaction", out var transaction) || transaction.ValueKind != JsonValueKind.Object)
            throw InvalidResponse();
        return ParseStatus(transaction);
    }

    public static GatewayStatus ParseStatus(JsonElement transaction)
    {
        var id = RequiredId(transaction, "id");
        var status = RequiredString(transaction, "status");
        var mode = RequiredString(transaction, "mode");
        var merchant = RequiredString(Required(transaction, "instance"), "name");
        var invoice = Required(transaction, "invoice");
        var currency = RequiredString(invoice, "currency");
        var invoiceTest = ReadTest(Required(invoice, "test"));
        var reference = invoice.TryGetProperty("referenceId", out var invoiceReference) && invoiceReference.ValueKind == JsonValueKind.String ? invoiceReference.GetString()! : RequiredString(transaction, "referenceId");
        var amount = ReadAmount(Required(transaction, "amount"));
        var gatewayId = OptionalId(invoice, "paymentRequestId");
        var originalId = OptionalId(transaction, "originalTransactionId");
        // The documented originalTransactionId is only guaranteed on completed
        // refund transactions; a refund-pending event can therefore lack it.
        var type = originalId is not null || status is "refund_pending" or "refund-pending" ? "credit" : "payment";
        var state = MapStatus(status, type);
        if (state is GatewayState.Paid or GatewayState.Authorized && (mode != "TEST" || !invoiceTest))
            throw InvalidResponse();
        var method = transaction.TryGetProperty("payment", out var payment) && payment.ValueKind == JsonValueKind.Object && payment.TryGetProperty("brand", out var brand) && brand.ValueKind == JsonValueKind.String ? brand.GetString() : null;
        return new GatewayStatus(id, merchant, reference, currency, amount, state == GatewayState.Paid && type == "payment" ? amount : null, type, status, state, method)
        {
            GatewayId = gatewayId,
            Mode = mode,
            InvoiceTest = invoiceTest,
            OriginalTransactionId = originalId
        };
    }

    public static GatewayState MapStatus(string status, string type = "payment") => status switch
    {
        "waiting" or "refund_pending" or "refund-pending" => GatewayState.Pending,
        "confirmed" when type == "payment" => GatewayState.Paid,
        "refunded" or "partially-refunded" when type == "credit" => GatewayState.Paid,
        "authorized" or "reserved" => GatewayState.Authorized,
        "cancelled" or "expired" => GatewayState.Cancelled,
        "declined" or "failed" or "error" => GatewayState.Failed,
        "chargeback" or "disputed" => GatewayState.Disputed,
        _ => GatewayState.Unknown
    };
    private async Task<JsonDocument> SendAsync(HttpMethod method, string path, object? body, CancellationToken ct)
    {
        var o = options.Value;
        var uri = new Uri($"{o.ApiBaseUrl.TrimEnd('/')}/{o.ApiVersion}/{path}?instance={Uri.EscapeDataString(o.InstanceName)}");
        using var request = new HttpRequestMessage(method, uri);
        request.Headers.Add("X-API-KEY", o.ApiKey);
        request.Headers.Accept.ParseAdd("application/json");
        if (body is not null)
            request.Content = JsonContent.Create(body);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(o.TimeoutSeconds));
        HttpResponseMessage response;
        try
        {
            response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, timeout.Token);
        }
        catch (OperationCanceledException)when (!ct.IsCancellationRequested)
        {
            throw new CommerceException("PAYMENT_PROVIDER_UNAVAILABLE", "The payment provider did not respond in time.", 502);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var status = (int)response.StatusCode;
                throw new CommerceException(status is >= 400 and < 500 and not 408 and not 409 and not 429 ? "PAYMENT_PROVIDER_REJECTED" : "PAYMENT_PROVIDER_UNAVAILABLE", "The payment provider could not complete this request.", 502);
            }

            if (response.Content.Headers.ContentLength is> 1_048_576)
                throw InvalidResponse();
            try
            {
                await using var stream = await response.Content.ReadAsStreamAsync(timeout.Token);
                // Bound chunked responses too; Content-Length is optional/untrusted.
                using var bounded = new MemoryStream();
                var buffer = new byte[8192];
                int count;
                while ((count = await stream.ReadAsync(buffer, timeout.Token)) != 0)
                {
                    if (bounded.Length + count > 1_048_576) throw InvalidResponse();
                    bounded.Write(buffer, 0, count);
                }
                return JsonDocument.Parse(bounded.ToArray(), new JsonDocumentOptions { MaxDepth = 64 });
            }
            catch (JsonException)
            {
                throw InvalidResponse();
            }
            catch (OperationCanceledException)when (!ct.IsCancellationRequested)
            {
                throw new CommerceException("PAYMENT_PROVIDER_UNAVAILABLE", "The payment provider did not respond in time.", 502);
            }
        }
    }

    private static JsonElement SingleData(JsonElement root)
    {
        if (root.ValueKind != JsonValueKind.Object || RequiredString(root, "status") != "success" || !root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array || data.GetArrayLength() != 1 || data[0].ValueKind != JsonValueKind.Object)
            throw InvalidResponse();
        return data[0];
    }

    private static JsonElement Required(JsonElement root, string name) => root.ValueKind == JsonValueKind.Object && root.TryGetProperty(name, out var value) && value.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) ? value : throw InvalidResponse();
    private static string RequiredString(JsonElement root, string name)
    {
        var value = Required(root, name);
        return value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString()) ? value.GetString()! : throw InvalidResponse();
    }

    private static string RequiredId(JsonElement root, string name) => OptionalId(root, name) ?? throw InvalidResponse();
    private static string? OptionalId(JsonElement root, string name)
    {
        if (!root.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null)
            return null;
        var id = value.ValueKind switch
        {
            JsonValueKind.Number when value.TryGetInt64(out var number) => number.ToString(System.Globalization.CultureInfo.InvariantCulture),
            JsonValueKind.String => value.GetString(),
            _ => null
        };
        if (id is null || !IdPattern.IsMatch(id))
            throw InvalidResponse();
        return id;
    }

    private static long ReadAmount(JsonElement value) => value.ValueKind == JsonValueKind.Number && value.TryGetInt64(out var amount) && amount >= 0 ? amount : throw InvalidResponse();
    private static bool ReadTest(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.Number when value.TryGetInt32(out var number) && number is 0 or 1 => number == 1,
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw InvalidResponse()};
    private static bool ReadBoolean(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        _ => throw InvalidResponse()};
    private static string? SingleGatewayTransactionId(JsonElement gateway)
    {
        if (!gateway.TryGetProperty("invoices", out var invoices) || invoices.ValueKind != JsonValueKind.Array)
            return null;
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var invoice in invoices.EnumerateArray())
        {
            if (invoice.ValueKind != JsonValueKind.Object || !invoice.TryGetProperty("transactions", out var transactions) || transactions.ValueKind != JsonValueKind.Array)
                continue;
            foreach (var transaction in transactions.EnumerateArray())
                ids.Add(RequiredId(transaction, "id"));
        }

        return ids.Count == 1 ? ids.Single() : null;
    }

    private static long Minor(decimal amount, string currency)
    {
        if (amount <= 0 || Money.Round(amount, currency) != amount)
            throw new CommerceException("PAYMENT_AMOUNT_INVALID", "Payment amount must use exact CHF cents.", 400);
        try
        {
            return Money.ToMinor(amount, currency);
        }
        catch (OverflowException)
        {
            throw new CommerceException("PAYMENT_AMOUNT_INVALID", "Payment amount exceeds the provider limit.", 400);
        }
    }

    private static void ValidateId(string id)
    {
        if (!IdPattern.IsMatch(id))
            throw InvalidResponse();
    }

    private static CommerceException InvalidResponse() => new("PAYMENT_PROVIDER_RESPONSE_INVALID", "The payment provider response could not be verified.", 502);
}
