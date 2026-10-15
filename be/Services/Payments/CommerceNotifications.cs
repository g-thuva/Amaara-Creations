using System.Globalization;
using be.Data;
using be.Models;

namespace be.Services.Payments;

public static class CommerceNotifications
{
    public static void Enqueue(ApplicationDbContext db, Order order, string eventKey, string subject, string detail)
    {
        string Value(decimal amount) => amount.ToString("0.00", CultureInfo.InvariantCulture) + " " + order.Currency;
        var lines = order.OrderItems.Select(i => $"{i.ProductNameSnapshot} x {i.Quantity}: {Value(i.Subtotal)}");
        var pickup = order.IsPickup ? $"\nPickup location: {order.PickupLocationSnapshot}" : string.Empty;
        var custom = order.OrderItems.Any(i => i.ItemType == OrderItemType.CustomDesign) ? "\nCustom items follow separate proof approval and production steps. Check proof status in your account." : string.Empty;
        var taxLabel = order.PricesIncludeTaxSnapshot ? "Tax (included)" : "Tax";
        var body = $"Order {order.OrderNumber}\nDate: {order.OrderDate:yyyy-MM-dd}\n{string.Join('\n', lines)}\nSubtotal: {Value(order.Subtotal)}\nDiscount: {Value(order.DiscountTotal)}\nShipping: {Value(order.ShippingTotal)}\n{taxLabel}: {Value(order.TaxTotal)}\nFee: {Value(order.PaymentFee)}\nTotal: {Value(order.Total)}\nDelivery: {order.ShippingMethodName}\nDestination: {order.ShippingCity}, {order.ShippingCountry}{pickup}{custom}\n{detail}";
        db.CommerceNotifications.Add(new CommerceNotification { OrderId = order.Id, EventKey = eventKey, Subject = $"{subject}: {order.OrderNumber}", Body = body });
    }
}
