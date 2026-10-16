using be.Domain.Orders;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace be.Domain.Shipping;
public class Shipment
{
    [Key]
    public long Id { get; set; }
    public int OrderId { get; set; }

    [StringLength(120)]
    public string? Method { get; set; }
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Pending;

    [StringLength(120)]
    public string? Courier { get; set; }

    [StringLength(120)]
    public string? TrackingNumber { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("OrderId")]
    public Order? Order { get; set; }

    [StringLength(500)]
    public string? TrackingUrl { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];
}
