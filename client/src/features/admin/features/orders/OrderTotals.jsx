import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
export function OrderTotals({ order }) {
  return (
    <>
      {" "}
      <p>
        Shipping: {order.shippingMethodName} ·{" "}
        {checkoutMoney(order.shippingTotal, order.currency)}
      </p>
      <p>
        Subtotal: {checkoutMoney(order.subtotal, order.currency)} · Discount:{" "}
        {checkoutMoney(order.discountTotal, order.currency)} {order.couponCode}
      </p>
      <p>
        Tax: {checkoutMoney(order.taxTotal, order.currency)} · Payment fee:{" "}
        {checkoutMoney(order.paymentFee, order.currency)}
      </p>
    </>
  );
}
