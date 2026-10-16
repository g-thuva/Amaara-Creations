namespace be.Domain.Shipping;
public static class ShipmentTransitionPolicy
{
    public static bool CanTransition(ShipmentStatus from, ShipmentStatus to, bool pickup) => (from, to) switch
    {
        (ShipmentStatus.Pending, ShipmentStatus.Packed) => !pickup,
        (ShipmentStatus.Pending, ShipmentStatus.ReadyForPickup) => pickup,
        (ShipmentStatus.Packed, ShipmentStatus.Shipped) => !pickup,
        (ShipmentStatus.Shipped, ShipmentStatus.Delivered) => !pickup,
        (ShipmentStatus.ReadyForPickup, ShipmentStatus.Delivered) => pickup,
        _ => false
    };
}
