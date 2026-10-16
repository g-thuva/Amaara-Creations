namespace be.Domain.Shipping;
public enum ShipmentStatus
{
    Pending = 0,
    Ready = 1,
    Shipped = 2,
    Delivered = 3,
    Failed = 4,
    Cancelled = 5,
    ReadyForPickup = 6,
    Packed = 7,
    Returned = 8
}
