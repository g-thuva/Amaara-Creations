using be.Data;
using be.DTOs.Checkout;
using be.Models;
using be.Services.Payments;
using Microsoft.EntityFrameworkCore;

namespace be.Services.Checkout;

public sealed class OrderLifecycleService(ApplicationDbContext db, InventoryReservationService inventory)
{
    public static bool Legal(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.PendingPayment, OrderStatus.Cancelled) => true,
        (OrderStatus.Pending, OrderStatus.Processing or OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.Processing) => true,
        (OrderStatus.Confirmed or OrderStatus.Processing or OrderStatus.PaymentReview, OrderStatus.Cancelled) => true,
        _ => false
    };
    public async Task TransitionAsync(int id, OrderStatus target, string actor, string? reason, CancellationToken ct)
    {
        await using var tx = await CommerceTransaction.BeginAsync(db, ct);
        var order = await db.Orders.Include(o => o.Payments).SingleOrDefaultAsync(o => o.Id == id, ct) ?? throw new CommerceException("ORDER_NOT_FOUND", "Order not found.", 404);
        if (!Legal(order.Status, target)) throw new CommerceException("INVALID_ORDER_TRANSITION", "This order transition is not allowed. Use shipment controls for delivery and payment controls for money movement.", 409);
        if (target == OrderStatus.Cancelled && order.Payments.Any(p => p.Status is PaymentStatus.Pending or PaymentStatus.Authorized or PaymentStatus.Paid or PaymentStatus.PartiallyRefunded))
            throw new CommerceException("PAYMENT_CANCELLATION_REQUIRED", "Resolve the payment before cancelling this order.", 409);
        if (target == OrderStatus.Cancelled) await inventory.ReleaseAsync(id, ct);
        History(order, target, actor, reason ?? "Administrator transition.");
        db.AuditLogs.Add(new AuditLog { ActorUserId = actor, Action = "OrderStatusChanged", EntityType = "Order", EntityId = id.ToString(), NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { Status = target.ToString(), Reason = reason }) });
        await db.SaveChangesAsync(ct); if (tx != null) await tx.CommitAsync(ct);
    }
    public async Task<Shipment> UpdateShipmentAsync(int orderId, string actor, ShipmentRequest request, CancellationToken ct)
    {
        if (!Enum.TryParse<ShipmentStatus>(request.Status, false, out var target) || !Enum.IsDefined(target)) throw new CommerceException("SHIPMENT_STATUS_INVALID", "Invalid shipment status.");
        if (request.TrackingUrl != null && (!Uri.TryCreate(request.TrackingUrl, UriKind.Absolute, out var url) || url.Scheme != "https" || !string.IsNullOrEmpty(url.UserInfo))) throw new CommerceException("TRACKING_URL_INVALID", "Tracking links must be absolute HTTPS URLs without credentials.");
        await using var tx = await CommerceTransaction.BeginAsync(db, ct);
        var order = await db.Orders.Include(o => o.OrderItems)!.ThenInclude(i => i.CustomDesign).SingleOrDefaultAsync(o => o.Id == orderId, ct) ?? throw new CommerceException("ORDER_NOT_FOUND", "Order not found.", 404);
        var shipment = await db.Shipments.SingleOrDefaultAsync(s => s.OrderId == orderId, ct) ?? new Shipment { OrderId = orderId, Method = order.ShippingMethodName };
        var metadataOnly = shipment.Id != 0 && shipment.Status == target;
        var progressAllowed = order.Status is OrderStatus.Confirmed or OrderStatus.Processing or OrderStatus.Shipped;
        var correctionAllowed = metadataOnly && order.Status is OrderStatus.Completed or OrderStatus.Delivered;
        if (!order.FinalizedAt.HasValue || !(progressAllowed || correctionAllowed)) throw new CommerceException("INVALID_ORDER_TRANSITION", "This order is not eligible for fulfilment.", 409);
        if (!metadataOnly && !LegalShipment(shipment.Status, target, order.IsPickup)) throw new CommerceException("INVALID_SHIPMENT_TRANSITION", "This shipment transition is not allowed.", 409);
        if (metadataOnly && string.IsNullOrWhiteSpace(request.Notes)) throw new CommerceException("SHIPMENT_REASON_REQUIRED", "Provide a reason for correcting shipment details.");
        if (order.OrderItems.Any(i => i.CustomDesign != null && (i.CustomDesign.ProductionStatus != ProductionStatus.Ready || i.CustomDesign.ProofStatus is not (ProofStatus.Approved or ProofStatus.NotRequired))))
            throw new CommerceException("CUSTOM_PRODUCTION_NOT_READY", "Complete custom production and proof approval before fulfilment.", 409);
        if (shipment.Id == 0) db.Shipments.Add(shipment);
        var oldValues = new { Status = shipment.Status.ToString(), shipment.Courier, shipment.TrackingNumber, shipment.TrackingUrl };
        var previous = shipment.Status; shipment.Status = target; shipment.Courier = request.Courier ?? shipment.Courier; shipment.TrackingNumber = request.TrackingNumber ?? shipment.TrackingNumber; shipment.TrackingUrl = request.TrackingUrl ?? shipment.TrackingUrl; shipment.UpdatedAt = DateTime.UtcNow;
        if (!metadataOnly && target == ShipmentStatus.Shipped) { shipment.ShippedAt ??= DateTime.UtcNow; CommerceNotifications.Enqueue(db, order, $"shipped:{orderId}", "Order shipped", $"Carrier: {shipment.Courier}\nTracking: {shipment.TrackingNumber}"); }
        if (!metadataOnly && target == ShipmentStatus.Delivered) { shipment.DeliveredAt ??= DateTime.UtcNow; History(order, OrderStatus.Completed, actor, "Delivery or collection confirmed."); }
        var eventNote = metadataOnly ? $"Shipment details corrected. {request.Notes}" : $"Fulfilment: {previous} to {target}. {request.Notes}";
        db.OrderStatusHistories.Add(new OrderStatusHistory { OrderId = orderId, PreviousStatus = order.Status.ToString(), NewStatus = order.Status.ToString(), ChangedByUserId = actor, Reason = eventNote[..Math.Min(500, eventNote.Length)] });
        db.AuditLogs.Add(new AuditLog { ActorUserId = actor, Action = metadataOnly ? "ShipmentTrackingChanged" : "ShipmentStatusChanged", EntityType = "Order", EntityId = orderId.ToString(), OldValuesJson = System.Text.Json.JsonSerializer.Serialize(oldValues), NewValuesJson = System.Text.Json.JsonSerializer.Serialize(new { Status = target.ToString(), shipment.Courier, shipment.TrackingNumber, shipment.TrackingUrl, request.Notes }) });
        await db.SaveChangesAsync(ct); if (tx != null) await tx.CommitAsync(ct);
        return shipment;
    }
    public static bool LegalShipment(ShipmentStatus from, ShipmentStatus to, bool pickup) => (from, to) switch
    {
        (ShipmentStatus.Pending, ShipmentStatus.Packed) => !pickup,
        (ShipmentStatus.Pending, ShipmentStatus.ReadyForPickup) => pickup,
        (ShipmentStatus.Packed, ShipmentStatus.Shipped) => !pickup,
        (ShipmentStatus.Shipped, ShipmentStatus.Delivered) => !pickup,
        (ShipmentStatus.ReadyForPickup, ShipmentStatus.Delivered) => pickup,
        _ => false
    };
    private static void History(Order order, OrderStatus target, string actor, string reason)
    {
        order.StatusHistory.Add(new OrderStatusHistory { PreviousStatus = order.Status.ToString(), NewStatus = target.ToString(), ChangedByUserId = actor, Reason = reason });
        order.Status = target; order.UpdatedAt = DateTime.UtcNow;
    }
}
