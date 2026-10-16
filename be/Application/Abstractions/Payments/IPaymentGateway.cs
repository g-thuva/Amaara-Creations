using be.Domain.Orders;
using be.Domain.Payments;

namespace be.Application.Abstractions.Payments;
public enum GatewayState
{
    Pending,
    Authorized,
    Paid,
    Failed,
    Cancelled,
    Disputed,
    Unknown
}

public enum GatewayLookupKind
{
    Gateway,
    Transaction
}

public record GatewayStatus(
        string TransactionId,
        string MerchantId,
        string Reference,
        string Currency,
        long? Amount,
        long? SettledAmount,
        string Type,
        string ProviderStatus,
        GatewayState State,
        string? Method)
{
    public string? GatewayId { get; init; }
    public string? Mode { get; init; }
    public bool? InvoiceTest { get; init; }
    public string? OriginalTransactionId { get; init; }
}

public record GatewayInitialization(string GatewayId, string RedirectUrl, string? TransactionId = null);
public record GatewayPaymentRequest(Payment Payment, Order Order, string Email, string? Phone, Guid IdempotencyKey);
public interface IPaymentGateway
{
    string Provider { get; }

    string MerchantId { get; }

    string Mode => "Sandbox";

    void EnsureAvailable(string currency);
    string RedirectUrl(string transactionId);
    Task<GatewayInitialization> InitializeAsync(GatewayPaymentRequest request, CancellationToken ct);
    Task<GatewayStatus> StatusAsync(string transactionId, CancellationToken ct);
    Task<GatewayStatus> StatusAsync(string id, GatewayLookupKind kind, CancellationToken ct) => StatusAsync(id, ct);
    Task CancelAsync(Payment payment, Guid idempotencyKey, CancellationToken ct);
    Task SettleAsync(Payment payment, Guid idempotencyKey, CancellationToken ct);
    Task<string> RefundAsync(Payment payment, Refund refund, CancellationToken ct);
}
