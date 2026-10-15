using System.Net;
using System.Text.Json;
using be.Models;
using be.Services.Checkout;
using be.Services.Payments;
using Microsoft.Extensions.Options;

namespace be.Tests;

public sealed class DatatransAdapterTests
{
    [Fact] public async Task InitializationUsesV1ServerAmountAndHostedRedirect()
    {
        var handler = new RecordingHandler("{\"transactionId\":\"260000000000000001\"}");
        var gateway = new DatatransPaymentGateway(new HttpClient(handler), Options.Create(Config()));
        var key = Guid.NewGuid();
        var result = await gateway.InitializeAsync(new(new Payment { Id = 7, Amount = 123.45m, Currency = "LKR", ProviderReference = "AC-test" }, new Order { ShippingCountry = "Sri Lanka", ShippingRecipientName = "Test customer" }, "test@example.test", "+94770000000", key), default);
        Assert.Equal("https://api.sandbox.datatrans.com/v1/transactions", handler.Uri); Assert.Equal(key.ToString(), handler.Key); Assert.True(handler.HasAuthentication);
        using var payload = JsonDocument.Parse(handler.Body!); Assert.Equal(12345, payload.RootElement.GetProperty("amount").GetInt64()); Assert.Equal("LKR", payload.RootElement.GetProperty("currency").GetString());
        Assert.Equal("VIS", payload.RootElement.GetProperty("paymentMethods")[0].GetString());
        Assert.Equal("https://pay.sandbox.datatrans.com/v1/start/260000000000000001", result.RedirectUrl);
        Assert.Contains("paymentId=7", payload.RootElement.GetProperty("redirect").GetProperty("successUrl").GetString());
        Assert.DoesNotContain("cardNumber", handler.Body); Assert.DoesNotContain("ApiPassword", handler.Body);
    }
    [Fact] public void DisabledGatewayFailsClosed()
    {
        var o = Config(); o.Enabled = false;
        Assert.Equal("PAYMENT_NOT_CONFIGURED", Assert.Throws<CommerceException>(() => Gateway(o).EnsureAvailable("LKR")).Code);
    }
    [Theory] [InlineData("merchant")] [InlineData("password")] [InlineData("hmac")] [InlineData("webhook")] [InlineData("frontend")] [InlineData("environment")]
    public void MissingOrUnsafeConfigurationFailsClosed(string field)
    {
        var o = Config();
        switch (field) { case "merchant": o.MerchantId = ""; break; case "password": o.ApiPassword = ""; break; case "hmac": o.HmacKey = ""; break; case "webhook": o.WebhookUrl = "http://hooks.example.test/callback"; break; case "frontend": o.FrontendBaseUrl = "https://user:password@example.test"; break; default: o.Environment = "Production"; break; }
        Assert.Equal("PAYMENT_NOT_CONFIGURED", Assert.Throws<CommerceException>(() => Gateway(o).EnsureAvailable("LKR")).Code);
    }
    [Fact] public void UnverifiedCurrencyFailsClosed()
    {
        Assert.Equal("PAYMENT_CURRENCY_UNSUPPORTED", Assert.Throws<CommerceException>(() => Gateway(Config()).EnsureAvailable("CHF")).Code);
    }
    [Fact] public void MissingMalformedOrDuplicatePaymentMethodsFailClosed()
    {
        foreach (var methods in new[] { Array.Empty<string>(), ["visa"], ["VIS "], ["VIS", "VIS"] })
        {
            var o = Config(); o.PaymentMethods = methods;
            Assert.Equal("PAYMENT_METHOD_UNVERIFIED", Assert.Throws<CommerceException>(() => Gateway(o).EnsureAvailable("LKR")).Code);
        }
    }
    [Fact] public void V1StatusReadsSettlementHistoryAndIgnoresCardSummary()
    {
        using var json = JsonDocument.Parse("{\"transactionId\":\"260000000000000001\",\"merchantId\":\"1000000000\",\"refno\":\"AC-test\",\"currency\":\"LKR\",\"type\":\"payment\",\"status\":\"settled\",\"detail\":{\"authorize\":{\"amount\":12345}},\"history\":[{\"action\":\"settle\",\"amount\":12345,\"success\":true}]}");
        var status = DatatransPaymentGateway.ParseStatus(json.RootElement); Assert.Equal(12345, status.Amount); Assert.Equal(12345, status.SettledAmount); Assert.Equal(GatewayState.Paid, status.State);
    }
    [Fact] public async Task V1CreditSendsMinorAmountAndParsesSettledCredit()
    {
        var handler = new RecordingHandler("{\"transactionId\":\"260000000000000002\"}");
        var gateway = new DatatransPaymentGateway(new HttpClient(handler), Options.Create(Config()));
        var refund = new Refund { Amount = 10, Currency = "LKR", ProviderReference = "ACR-test", IdempotencyKey = Guid.NewGuid() };
        var id = await gateway.RefundAsync(new Payment { ProviderTransactionId = "260000000000000001" }, refund, default);
        Assert.Equal("260000000000000002", id); Assert.EndsWith("/v1/transactions/260000000000000001/credit", handler.Uri); Assert.Equal(refund.IdempotencyKey.ToString(), handler.Key);
        using var body = JsonDocument.Parse(handler.Body!); Assert.Equal(1000, body.RootElement.GetProperty("amount").GetInt64()); Assert.Equal("LKR", body.RootElement.GetProperty("currency").GetString()); Assert.Equal(refund.ProviderReference, body.RootElement.GetProperty("refno").GetString());
        using var json = JsonDocument.Parse("{\"transactionId\":\"260000000000000002\",\"merchantId\":\"1000000000\",\"refno\":\"ACR-test\",\"currency\":\"LKR\",\"type\":\"credit\",\"status\":\"settled\",\"detail\":{\"credit\":{\"amount\":1000}}}");
        var state = DatatransPaymentGateway.ParseStatus(json.RootElement); Assert.Equal("credit", state.Type); Assert.Equal(1000, state.Amount); Assert.Equal(GatewayState.Paid, state.State);
    }
    private static DatatransPaymentGateway Gateway(DatatransOptions options) => new(new HttpClient(new RecordingHandler("{}")), Options.Create(options));
    private static DatatransOptions Config() => new() { Enabled = true, MerchantId = "1000000000", ApiPassword = "deterministic-test-only", HmacKey = Convert.ToHexString(Enumerable.Repeat((byte)7, 32).ToArray()), VerifiedCurrencies = ["LKR"], PaymentMethods = ["VIS"], WebhookUrl = "https://hooks.example.test/api/v1/payments/datatrans/webhook", FrontendBaseUrl = "http://localhost:5173/Amaara-Creations" };
    private sealed class RecordingHandler(string response) : HttpMessageHandler
    {
        public string? Uri { get; private set; } public string? Body { get; private set; } public string? Key { get; private set; } public bool HasAuthentication { get; private set; }
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            Uri = request.RequestUri!.ToString(); Body = request.Content == null ? null : await request.Content.ReadAsStringAsync(ct); Key = request.Headers.GetValues("Idempotency-Key").Single(); HasAuthentication = request.Headers.Authorization?.Scheme == "Basic";
            return new(HttpStatusCode.Created) { Content = new StringContent(response) };
        }
    }
}
