import { formatDateTime } from "@/shared/utils/formatDate.ts";
export function OrderTimeline({ order }) {
  return (
    <section className="card">
      <div className="card-header">
        <h3>Order history</h3>
      </div>
      <ul>
        {order.history.map((h, i) => (
          <li key={i}>
            <time>{formatDateTime(h.createdAt)}</time> · {h.previousStatus} →{" "}
            {h.newStatus}: {h.reason}
          </li>
        ))}
      </ul>
    </section>
  );
}
