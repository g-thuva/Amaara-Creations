using be.Domain.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Promotions;
public class PromotionRedemption
{
    public long Id { get; set; }
    public int PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null !;
    public int OrderId { get; set; }
    public Order Order { get; set; } = null !;

    [StringLength(450)]
    public string UserId { get; set; } = string.Empty;

    [StringLength(20)]
    public string Status { get; set; } = "Reserved";

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
