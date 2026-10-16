namespace be.Domain.Orders;
public static class OrderTransitionPolicy
{
    public static bool CanTransition(OrderStatus from, OrderStatus to) => (from, to) switch
    {
        (OrderStatus.PendingPayment, OrderStatus.Cancelled) => true,
        (OrderStatus.Pending, OrderStatus.Processing or OrderStatus.Cancelled) => true,
        (OrderStatus.Confirmed, OrderStatus.Processing) => true,
        (OrderStatus.Confirmed or OrderStatus.Processing or OrderStatus.PaymentReview, OrderStatus.Cancelled) => true,
        _ => false
    };
}
