namespace be.Domain.Orders;
public enum OrderStatus
{
    Pending = 0,
    Processing = 1,
    Shipped = 2,
    Delivered = 3,
    Cancelled = 4,
    PendingPayment = 5,
    Confirmed = 6,
    Completed = 7,
    PaymentReview = 8
}

public enum OrderItemType
{
    Product = 0,
    CustomDesign = 1
}
