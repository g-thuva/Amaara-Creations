import { SectionCard } from "../../shared/components/SectionCard.jsx";
import { formatDateTime } from "../../shared/utils/formatDateTime.js";
import { OrderTotals } from "./OrderTotals.jsx";
export function OrderSummary({ order }) {
  return (
    <SectionCard title="Order snapshot" inline>
      <p>Created: {formatDateTime(order.createdAt)}</p>
      <p>
        Customer: {order.userName} · {order.userEmail}
      </p>
      <p>
        Recipient: {order.shippingRecipientName || "Not recorded"} ·{" "}
        {order.shippingPhone || "No phone recorded"}
      </p>
      <address>
        {order.shippingAddress}
        <br />
        {order.shippingCity} {order.shippingRegion} {order.shippingPostalCode}
        <br />
        {order.shippingCountry}
      </address>
      <OrderTotals order={order} />
      <p>Order notes: {order.notes || "None"}</p>
    </SectionCard>
  );
}
