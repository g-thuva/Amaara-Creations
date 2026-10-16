using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Promotions;
public class Promotion
{
    [Key]
    public int Id { get; set; }

    [Required]
    [StringLength(40)]
    public string Code { get; set; } = string.Empty;

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;
    public DiscountType DiscountType { get; set; } = DiscountType.FixedAmount;

    [Column(TypeName = "decimal(18,2)")]
    public decimal DiscountValue { get; set; }
    public DateTime StartsAt { get; set; }
    public DateTime? EndsAt { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MinimumSpend { get; set; }
    public int? UsageLimit { get; set; }
    public int UsageCount { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [StringLength(3)]
    public string Currency { get; set; } = "LKR";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaximumDiscount { get; set; }
    public int? PerCustomerUsageLimit { get; set; }
    public bool AppliesToStandard { get; set; } = true;
    public bool AppliesToCustom { get; set; } = true;

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
