using System.ComponentModel.DataAnnotations;
namespace be.Application.Features.Payments;
public record RefundStatusDto(
        long Id,
        decimal Amount,
        string Currency,
        string Status,
        string? Reason,
        DateTime CreatedAt,
        string? ProviderTransactionId);
public record PaymentEventDto(string ProviderStatus, DateTime ProcessedAt);
public sealed class PaymentStatusDto
{
    public long Id { get; init; }
    public int OrderId { get; init; }
    public string Provider { get; init; } = string.Empty;
    public string? ProviderMode { get; init; }
    public string? ProviderStatus { get; init; }
    public string? ProviderGatewayId { get; init; }
    public string Status { get; init; } = string.Empty;
    public string OrderStatus { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal AuthorizedAmount { get; init; }
    public decimal SettledAmount { get; init; }
    public decimal RefundedAmount { get; init; }
    public decimal RefundableAmount { get; init; }
    public string? ProviderReference { get; init; }
    public string? ProviderTransactionId { get; init; }
    public DateTime? PaidAt { get; init; }
    public DateTime CreatedAt { get; init; }
    public int AttemptNumber { get; init; }
    public DateTime? LastReconciledAt { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public string? RedirectUrl { get; init; }
    public bool UnableToVerify { get; init; }
    public bool ProviderAvailable { get; init; }
    public IReadOnlyList<PaymentEventDto> Reconciliations { get; init; } = [];
    public IReadOnlyList<RefundStatusDto> Refunds { get; init; } = [];
    public IReadOnlyList<PaymentEventDto> Events { get; init; } = [];
}

public record PaymentSessionDto(int OrderId, long PaymentId, string Status, string? RedirectUrl, DateTime? ExpiresAt);
public record RetryPaymentRequest(Guid IdempotencyKey);
public record RefundRequest(
        [Range(typeof(decimal), "0.01", "9999999999999999")] decimal Amount,
        [Required, StringLength(500)] string Reason,
        Guid IdempotencyKey);
