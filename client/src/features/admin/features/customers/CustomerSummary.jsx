import { formatLocalDate as formatDate } from "@/features/admin/shared/utils/format.js";
import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
export function CustomerSummary({ data }) {
  return (
    <section className="card card-body">
      <h2>{data.customer.name}</h2>
      <p>
        {data.customer.email} · {data.customer.phone}
      </p>
      <p>
        Email: {data.customer.emailConfirmed ? "Verified" : "Unverified"} ·
        Account: {data.customer.locked ? "Locked" : "Active"}
      </p>
      <p>
        Joined: {formatDate(data.customer.createdAt)} · {data.orderCount} orders
      </p>
      <h3>Lifetime settled order value</h3>
      {data.lifetimeValue.length ? (
        data.lifetimeValue.map((v) => (
          <p key={`${v.currency}-${v.providerMode}`}>
            {checkoutMoney(v.settled - v.refunded, v.currency)} ·{" "}
            {v.providerMode || "Historical mode unspecified"} · Refunds{" "}
            {checkoutMoney(v.refunded, v.currency)}
          </p>
        ))
      ) : (
        <p>No settled payments.</p>
      )}
    </section>
  );
}
