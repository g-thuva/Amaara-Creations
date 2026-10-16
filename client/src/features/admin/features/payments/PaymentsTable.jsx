import { formatDateTime } from "@/shared/utils/formatDate.ts";
import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
import { Link } from "react-router-dom";
export function PaymentsTable({ resource }) {
  return (
    <DataTable
      caption="Payment attempts"
      rows={resource.data?.items || []}
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
        { key: "providerGatewayId", label: "Gateway" },
        { key: "providerReference", label: "Reference" },
      ]}
    />
  );
}
