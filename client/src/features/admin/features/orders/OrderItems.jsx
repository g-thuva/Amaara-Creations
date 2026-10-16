import { Link } from "react-router-dom";
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
import { SnapshotViewer } from "@/features/admin/shared/components/SnapshotViewer.jsx";
export function OrderItems({ order, canManageOrders }) {
  return (
    <section className="card card-body">
      <h2>Item snapshots</h2>
      {order.orderItems.map((i) => (
        <article key={i.id}>
          <h3>{i.productName}</h3>
          <p>
            {i.itemType} · SKU: {i.sku || "Not recorded"} · Quantity:{" "}
            {i.quantity}
          </p>
          <p>
            Unit: {checkoutMoney(i.price, order.currency)} · Line:{" "}
            {checkoutMoney(i.subtotal, order.currency)}
          </p>
          {i.customDesignId && canManageOrders && (
            <Link to={`/admin/custom-designs/${i.customDesignId}`}>
              Production and private artwork
            </Link>
          )}
          {i.configurationSnapshotJson && (
            <details>
              <summary>Immutable custom production specification</summary>
              <SnapshotViewer value={i.configurationSnapshotJson} />
            </details>
          )}
        </article>
      ))}
    </section>
  );
}
