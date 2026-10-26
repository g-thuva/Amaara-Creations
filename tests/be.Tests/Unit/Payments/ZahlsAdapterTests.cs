using be.Domain.Common;
using be.Application.Abstractions.Payments;
using be.Application.Features.Checkout;
using be.Domain.Orders;
using be.Domain.Payments;
using be.Infrastructure.Payments.Zahls;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace be.Tests;
public sealed class ZahlsAdapterTests
{
    [Fact]
    public async Task CreatesHostedGatewayWithExactChfCentsAndPersistableIdentity()
    {
        var handler = new Responses(GatewayResponse);
        var gateway = Gateway(Config(), handler);
        var order = new Order
        {
            Currency = "CHF",
            Total = 25.50m,
            OrderNumber = "ORD-TEST-1",
            ShippingTotal = 5m,
            OrderItems = [new OrderItem
            {
                ProductNameSnapshot = "TEST product",
                UnitPrice = 20.50m,
                Quantity = 1
            }

            ]
        };
        var payment = new Payment
        {
            Id = 7,
            Amount = 25.50m,
            Currency = "CHF",
            ProviderReference = "AC-test"
        };
        var result = await gateway.InitializeAsync(new(payment, order, "test@example.test", "+41440000000", Guid.NewGuid()), default);
        Assert.Equal("123", result.GatewayId);
        Assert.Equal("https://pay.zahls.ch/test-link", result.RedirectUrl);
        Assert.Null(result.TransactionId);
        Assert.Equal("https://api.zahls.ch/v1.16/Gateway/?instance=testmerchant", handler.Requests[0].Uri);
        Assert.Equal("fixture-api-key", handler.Requests[0].ApiKey);
        Assert.Equal("application/json", handler.Requests[0].ContentType);
        using var body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal(2550, body.RootElement.GetProperty("amount").GetInt64());
        Assert.Equal("CHF", body.RootElement.GetProperty("currency").GetString());
        Assert.Equal("AC-test", body.RootElement.GetProperty("referenceId").GetString());
        Assert.Equal("test-card-fixture", body.RootElement.GetProperty("pm")[0].GetString());
        Assert.Equal(2050, body.RootElement.GetProperty("basket")[0].GetProperty("amount").GetInt64());
        Assert.Equal(500, body.RootElement.GetProperty("basket")[1].GetProperty("amount").GetInt64());
        Assert.Contains("paymentId=7", body.RootElement.GetProperty("successRedirectUrl").GetString());
        Assert.DoesNotContain("fixture-api-key", handler.Requests[0].Body);
    }

    [Fact]
    public async Task DiscountedOrderOmitsAnInconsistentOptionalBasket()
    {
        var handler = new Responses(GatewayResponse.Replace("2550", "2000"));
        var order = new Order
        {
            Currency = "CHF",
            Total = 20m,
            Subtotal = 25m,
            DiscountTotal = 5m,
            OrderNumber = "ORD-DISCOUNT",
            OrderItems = [new OrderItem
            {
                ProductNameSnapshot = "TEST product",
                UnitPrice = 25m,
                Quantity = 1
            }

            ]
        };
        var payment = new Payment
        {
            Id = 8,
            Amount = 20m,
            Currency = "CHF",
            ProviderReference = "AC-test"
        };
        await Gateway(Config(), handler).InitializeAsync(new(payment, order, "test@example.test", null, Guid.NewGuid()), default);
        using var body = JsonDocument.Parse(handler.Requests[0].Body!);
        Assert.Equal(2000, body.RootElement.GetProperty("amount").GetInt64());
        Assert.False(body.RootElement.TryGetProperty("basket", out _));
    }

    [Theory]
    [InlineData("disabled")]
    [InlineData("live")]
    [InlineData("instance")]
    [InlineData("key")]
    [InlineData("signing")]
    [InlineData("psp")]
    [InlineData("chf")]
    [InlineData("webhook")]
    [InlineData("frontend")]
    [InlineData("api")]
    public void IncompleteOrLiveConfigurationFailsClosed(string caseName)
    {
        var o = Config();
        switch (caseName)
        {
            case "disabled":
                o.Enabled = false;
                break;
            case "live":
                o.Mode = "LIVE";
                break;
            case "instance":
                o.InstanceName = "";
                break;
            case "key":
                o.ApiKey = "";
                break;
            case "signing":
                o.WebhookSigningKey = "";
                break;
            case "psp":
                o.TestProviderConfigured = false;
                break;
            case "chf":
                o.ChfSupportedByTestProvider = false;
                break;
            case "webhook":
                o.WebhookUrl = "http://example.test/hook";
                break;
            case "frontend":
                o.FrontendBaseUrl = "https://user:password@example.test";
                break;
            case "api":
                o.ApiBaseUrl = "https://api.example.test";
                break;
        }

        Assert.Equal("PAYMENT_NOT_CONFIGURED", Assert.Throws<CommerceException>(() => Gateway(o).EnsureAvailable("CHF")).Code);
    }

    [Fact]
    public void MalformedMethodsAndLkrFailClosed()
    {
        foreach (var methods in new string[][]
        {
            ["bad method"],
            ["test-card-fixture", "TEST-CARD-FIXTURE"]
        }

        )
        {
            var o = Config();
            o.PaymentMethods = methods;
            Assert.Equal("PAYMENT_METHOD_UNVERIFIED", Assert.Throws<CommerceException>(() => Gateway(o).EnsureAvailable("CHF")).Code);
        }

        Assert.Equal("PAYMENT_NOT_CONFIGURED", Assert.Throws<CommerceException>(() => Gateway(Config()).EnsureAvailable("LKR")).Code);
    }

    [Fact]
    public async Task EmptyOptionalFilterOmitsPmButNeverBypassesProviderReadiness()
    {
        var o = Config(); o.PaymentMethods = [];
        var handler = new Responses(GatewayResponse);
        var order = new Order { Currency = "CHF", Total = 25.50m, OrderNumber = "ORD-TEST" };
        var payment = new Payment { Id = 7, Amount = 25.50m, Currency = "CHF", ProviderReference = "AC-test" };
        await Gateway(o, handler).InitializeAsync(new(payment, order, "test@example.test", null, Guid.NewGuid()), default);
        using var body = JsonDocument.Parse(handler.Requests.Single().Body!);
        Assert.False(body.RootElement.TryGetProperty("pm", out _));
        o.TestProviderConfigured = false;
        Assert.Throws<CommerceException>(() => Gateway(o).EnsureAvailable("CHF"));
        o.TestProviderConfigured = true; o.ChfSupportedByTestProvider = false;
        Assert.Throws<CommerceException>(() => Gateway(o).EnsureAvailable("CHF"));
    }

    [Theory]
    [InlineData("https://hooks.example.test/swagger")]
    [InlineData("https://hooks.example.test/api/v1/payments/zahls/webhook?token=secret")]
    [InlineData("https://hooks.example.test:8443/api/v1/payments/zahls/webhook")]
    public void WebhookMustBeTheExactPublicHttpsCallback(string url)
    {
        var o = Config(); o.WebhookUrl = url;
        Assert.Throws<CommerceException>(() => Gateway(o).EnsureAvailable("CHF"));
    }

    [Fact]
    public async Task FlatMarketingResponseOrUntrustedHostedLinkCannotStartCheckout()
    {
        var order = new Order
        {
            Currency = "CHF",
            Total = 10m,
            OrderNumber = "TEST"
        };
        var payment = new Payment
        {
            Id = 1,
            Currency = "CHF",
            Amount = 10m,
            ProviderReference = "AC-test"
        };
        foreach (var response in new[]
        {
            "{\"status\":\"success\",\"link\":\"https://pay.zahls.ch/example\"}",
            "{\"status\":\"success\",\"data\":[{\"id\":123,\"link\":\"https://evil.example/pay\"}]}"
        }

        )
        {
            var code = (await Assert.ThrowsAsync<CommerceException>(() => Gateway(Config(), new Responses(response)).InitializeAsync(new(payment, order, "test@example.test", null, Guid.NewGuid()), default))).Code;
            Assert.Contains(code, new[] { "PAYMENT_PROVIDER_RESPONSE_INVALID", "PAYMENT_REDIRECT_INVALID" });
        }
    }

    [Fact]
    public async Task GatewayConfirmationRequiresMatchingTransactionWithTestEvidence()
    {
        var handler = new Responses("{\"status\":\"success\",\"data\":[{\"id\":123,\"status\":\"confirmed\",\"referenceId\":\"AC-test\",\"amount\":2550,\"currency\":\"CHF\",\"invoices\":[{\"transactions\":[{\"id\":456}]}]}]}", TransactionResponse);
        var state = await Gateway(Config(), handler).StatusAsync("123", GatewayLookupKind.Gateway, default);
        Assert.Equal(GatewayState.Paid, state.State);
        Assert.Equal("123", state.GatewayId);
        Assert.Equal("456", state.TransactionId);
        Assert.Equal("TEST", state.Mode);
        Assert.True(state.InvoiceTest);
        Assert.Equal("visa", state.Method);
        Assert.Equal("https://api.zahls.ch/v1.16/Transaction/456/?instance=testmerchant", handler.Requests[1].Uri);
    }

    [Fact]
    public async Task GatewayWithNoTransactionNeverSynthesizesPaidState()
    {
        var handler = new Responses("{\"status\":\"success\",\"data\":[{\"id\":123,\"status\":\"confirmed\",\"referenceId\":\"AC-test\",\"amount\":2550,\"currency\":\"CHF\",\"invoices\":[]}]}");
        var state = await Gateway(Config(), handler).StatusAsync("123", GatewayLookupKind.Gateway, default);
        Assert.Equal(GatewayState.Unknown, state.State);
        Assert.Empty(state.TransactionId);
    }

    [Fact]
    public void SignedWebhookMapsExactTestStateAndRejectsLiveSuccess()
    {
        using var json = JsonDocument.Parse("{\"transaction\":{\"id\":456,\"status\":\"confirmed\",\"mode\":\"TEST\",\"instance\":{\"name\":\"testmerchant\"},\"amount\":2550,\"referenceId\":\"AC-test\",\"invoice\":{\"currency\":\"CHF\",\"test\":1,\"referenceId\":\"AC-test\",\"paymentRequestId\":123},\"payment\":{\"brand\":\"visa\"}}}");
        var state = ZahlsPaymentGateway.ParseWebhook(json.RootElement);
        Assert.Equal(GatewayState.Paid, state.State);
        Assert.Equal("123", state.GatewayId);
        Assert.Equal("456", state.TransactionId);
        Assert.Equal(2550, state.Amount);
        Assert.Equal(2550, state.SettledAmount);
        using var live = JsonDocument.Parse(json.RootElement.GetRawText().Replace("\"TEST\"", "\"LIVE\""));
        Assert.Equal("PAYMENT_PROVIDER_RESPONSE_INVALID", Assert.Throws<CommerceException>(() => ZahlsPaymentGateway.ParseWebhook(live.RootElement)).Code);
    }

    [Fact]
    public void PendingRefundWithoutOriginalIdRemainsAUncompletedCredit()
    {
        using var json = JsonDocument.Parse(TransactionResponse.Replace("\"status\":\"confirmed\"", "\"status\":\"refund_pending\"").Replace("\"id\":456", "\"id\":789"));
        var state = ZahlsPaymentGateway.ParseStatus(json.RootElement.GetProperty("data")[0]);
        Assert.Equal("credit", state.Type);
        Assert.Equal(GatewayState.Pending, state.State);
        Assert.Null(state.OriginalTransactionId);
    }

    [Theory]
    [InlineData("waiting", "payment", GatewayState.Pending)]
    [InlineData("confirmed", "payment", GatewayState.Paid)]
    [InlineData("cancelled", "payment", GatewayState.Cancelled)]
    [InlineData("declined", "payment", GatewayState.Failed)]
    [InlineData("expired", "payment", GatewayState.Cancelled)]
    [InlineData("reserved", "payment", GatewayState.Authorized)]
    [InlineData("refund_pending", "credit", GatewayState.Pending)]
    [InlineData("refund-pending", "credit", GatewayState.Pending)]
    [InlineData("partially-refunded", "credit", GatewayState.Paid)]
    [InlineData("refunded", "payment", GatewayState.Unknown)]
    [InlineData("disputed", "payment", GatewayState.Disputed)]
    [InlineData("chargeback", "payment", GatewayState.Disputed)]
    public void ProviderStatusesStayDistinct(string status, string type, GatewayState expected) => Assert.Equal(expected, ZahlsPaymentGateway.MapStatus(status, type));
    [Fact]
    public async Task RefundUsesOriginalTransactionAndExactMinorAmount()
    {
        var handler = new Responses(TransactionResponse, "{\"status\":\"success\",\"data\":[{\"id\":789}]}");
        var payment = new Payment
        {
            Provider = "Zahls",
            ProviderMode = "TEST",
            Currency = "CHF",
            Amount = 25.50m,
            SettledAmount = 25.50m,
            ProviderReference = "AC-test",
            ProviderTransactionId = "456"
        };
        var refund = new Refund
        {
            Currency = "CHF",
            Amount = 5m,
            ProviderReference = "RF-test",
            IdempotencyKey = Guid.NewGuid()
        };
        var id = await Gateway(Config(), handler).RefundAsync(payment, refund, default);
        Assert.Equal("789", id);
        Assert.Equal("https://api.zahls.ch/v1.16/Transaction/456/refund/?instance=testmerchant", handler.Requests[1].Uri);
        using var body = JsonDocument.Parse(handler.Requests[1].Body!);
        Assert.Equal(500, body.RootElement.GetProperty("amount").GetInt64());
        Assert.Single(body.RootElement.EnumerateObject());
    }

    [Fact]
    public async Task TwoSequentialPartialRefundsVerifyTheProvidersUpdatedBalance()
    {
        var afterFirst = TransactionResponse.Replace("\"status\":\"confirmed\"", "\"status\":\"partially-refunded\"").Replace("\"refundedAmount\":0", "\"refundedAmount\":500");
        var handler = new Responses(TransactionResponse, "{\"status\":\"success\",\"data\":[{\"id\":789}]}", afterFirst, "{\"status\":\"success\",\"data\":[{\"id\":790}]}");
        var payment = new Payment
        {
            Provider = "Zahls",
            ProviderMode = "TEST",
            Currency = "CHF",
            Amount = 25.50m,
            SettledAmount = 25.50m,
            ProviderReference = "AC-test",
            ProviderTransactionId = "456"
        };
        var gateway = Gateway(Config(), handler);
        var first = new Refund
        {
            Currency = "CHF",
            Amount = 5m
        };
        Assert.Equal("789", await gateway.RefundAsync(payment, first, default));
        payment.RefundedAmount = 5m; // The backend applies only a confirmed first refund.
        var second = new Refund
        {
            Currency = "CHF",
            Amount = 4m
        };
        Assert.Equal("790", await gateway.RefundAsync(payment, second, default));
        using var body = JsonDocument.Parse(handler.Requests[3].Body!);
        Assert.Equal(400, body.RootElement.GetProperty("amount").GetInt64());
    }

    [Fact]
    public async Task RefundStopsWhenProviderBalanceDiffersFromTheBackend()
    {
        var providerAhead = TransactionResponse.Replace("\"refundedAmount\":0", "\"refundedAmount\":500");
        var payment = new Payment
        {
            Provider = "Zahls",
            ProviderMode = "TEST",
            Currency = "CHF",
            Amount = 25.50m,
            SettledAmount = 25.50m,
            ProviderReference = "AC-test",
            ProviderTransactionId = "456"
        };
        var handler = new Responses(providerAhead);
        var error = await Assert.ThrowsAsync<CommerceException>(() => Gateway(Config(), handler).RefundAsync(payment, new Refund { Currency = "CHF", Amount = 4m }, default));
        Assert.Equal("REFUND_VERIFICATION_FAILED", error.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public void WebhookSignatureUsesExactRawBytesAndSeparateUtf8Key()
    {
        var o = Config();
        var verifier = new ZahlsWebhookVerifier(Options.Create(o));
        var raw = Encoding.UTF8.GetBytes("{\"transaction\":{\"id\":1}}\n");
        var signature = Convert.ToHexString(HMACSHA256.HashData(Encoding.UTF8.GetBytes(o.WebhookSigningKey), raw)).ToLowerInvariant();
        Assert.True(verifier.Verify(raw, signature));
        Assert.False(verifier.Verify(Encoding.UTF8.GetBytes("{\"transaction\":{\"id\":1}}"), signature));
        Assert.False(verifier.Verify(raw, signature.ToUpperInvariant()));
        Assert.False(verifier.Verify(raw, null));
        o.Mode = "LIVE";
        Assert.False(verifier.Verify(raw, signature));
    }

    private const string GatewayResponse = "{\"status\":\"success\",\"data\":[{\"id\":123,\"link\":\"https://pay.zahls.ch/test-link\",\"amount\":2550,\"currency\":\"CHF\",\"referenceId\":\"AC-test\"}]}";
    private const string TransactionResponse = "{\"status\":\"success\",\"data\":[{\"id\":456,\"status\":\"confirmed\",\"mode\":\"TEST\",\"instance\":{\"name\":\"testmerchant\"},\"amount\":2550,\"referenceId\":\"AC-test\",\"invoice\":{\"currency\":\"CHF\",\"test\":1,\"referenceId\":\"AC-test\",\"paymentRequestId\":123,\"refundedAmount\":0},\"payment\":{\"brand\":\"visa\"},\"refundable\":true,\"partiallyRefundable\":true}]}";
    private static ZahlsOptions Config() => new()
    {
        Enabled = true,
        InstanceName = "testmerchant",
        ApiKey = "fixture-api-key",
        WebhookSigningKey = "fixture-signing-key",
        FrontendBaseUrl = "http://localhost:5173/Amaara-Creations",
        WebhookUrl = "https://hooks.example.test/api/v1/payments/zahls/webhook",
        PaymentMethods = ["test-card-fixture"],
        TestProviderConfigured = true,
        ChfSupportedByTestProvider = true
    };
    private static ZahlsPaymentGateway Gateway(ZahlsOptions config, Responses? handler = null) => new(new HttpClient(handler ?? new Responses(GatewayResponse)), Options.Create(config));
    private sealed class Responses(params string[] responses) : HttpMessageHandler
    {
        private int index;
        public List<(string Uri, string? Body, string? ApiKey, string? ContentType)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            Requests.Add((request.RequestUri!.AbsoluteUri, body, request.Headers.GetValues("X-API-KEY").Single(), request.Content?.Headers.ContentType?.MediaType));
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responses[index++], Encoding.UTF8, "application/json")
            };
        }
    }
}
