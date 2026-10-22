import { formatDateTime } from "@/features/admin/shared/utils/format.js";
import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
import { Link } from "react-router-dom";
import { CopyableId } from '@/features/admin/shared/components/AdminUi.jsx';
export function PaymentsTable({ resource, reset }) {
  return (
    <DataTable
      caption="Payment attempts"
      rows={resource.data?.items || []}
      empty="No payment attempts match these filters"
      emptyDescription="Payment attempts appear after a customer starts checkout. Try another filter to see earlier attempts."
      emptyAction={reset && <button className="btn btn-outline-primary" onClick={reset}>Reset filters</button>}
      columns={[
        {
          key: "orderNumber",
          label: "Order and operations",
          render: (r) => (
            <Link to={`/admin/orders/${r.orderId}`}>{r.orderNumber}</Link>
          ),
        },
        { key: "provider", label: "Provider" },
        { key: "providerMode", label: "Mode" },
        { key: "status", label: "State" },
        {
          key: "amount",
          label: "Amount",
          render: (r) => checkoutMoney(r.amount, r.currency),
        },
        {
          key: "createdAt",
          label: "Created",
          render: (r) => formatDateTime(r.createdAt),
        },
        {
          key: "paidAt",
          label: "Confirmed",
          render: (r) => (r.paidAt ? formatDateTime(r.paidAt) : "Unconfirmed"),
        },
        { key: "providerGatewayId", label: "Gateway", render: r => <CopyableId value={r.providerGatewayId} label="gateway identifier" /> },
        { key: "providerReference", label: "Reference", render: r => <CopyableId value={r.providerReference} label="payment reference" /> },
      ]}
    />
  );
}
