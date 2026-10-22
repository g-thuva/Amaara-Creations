import { formatDateTime } from "@/features/admin/shared/utils/format.js";
import { Timeline } from '@/features/admin/shared/components/AdminUi.jsx';
export function OrderTimeline({ order }) {
  return (
    <section className="card">
      <div className="card-header">
        <h2>Order history</h2>
      </div>
      <Timeline label="Order history">
        {order.history.map((h, i) => (
          <li key={i}>
            <time>{formatDateTime(h.createdAt)}</time> · {h.previousStatus} →{" "}
            {h.newStatus}: {h.reason}
          </li>
        ))}
      </Timeline>
    </section>
  );
}
