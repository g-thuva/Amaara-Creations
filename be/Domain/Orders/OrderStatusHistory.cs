using be.Domain.Identity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Orders;
public class OrderStatusHistory
{
    [Key]
    public long Id { get; set; }
    public int OrderId { get; set; }

    [StringLength(40)]
    public string? PreviousStatus { get; set; }

    [Required]
    [StringLength(40)]
    public string NewStatus { get; set; } = string.Empty;
    public string? ChangedByUserId { get; set; }

    [StringLength(500)]
    public string? Reason { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [ForeignKey("ChangedByUserId")]
    public User? ChangedByUser { get; set; }
}
