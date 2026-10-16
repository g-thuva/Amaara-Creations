using System.ComponentModel.DataAnnotations;

namespace be.Application.Features.Promotions;
public sealed record PromotionRequest(
        [Required, RegularExpression("^[A-Za-z0-9_-]{1,40}$")] string Code,
        [Required, StringLength(160)] string Name,
        string DiscountType,
        decimal DiscountValue,
        string Currency,
        DateTime StartsAt,
        DateTime? EndsAt,
        decimal? MinimumSpend,
        decimal? MaximumDiscount,
        int? UsageLimit,
        int? PerCustomerUsageLimit,
        bool IsActive,
        bool AppliesToStandard,
        bool AppliesToCustom,
        string? RowVersion);

public record CouponResultDto(string Code, bool Applied, string? ErrorCode, string? Message, decimal Discount);
