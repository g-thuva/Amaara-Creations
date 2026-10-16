import { formatDateTime } from "@/shared/utils/formatDate.ts";
import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
import { Link } from "react-router-dom";
export function OrdersTable({ resource }) {
  return (
    <DataTable
      caption="Customer orders"
      rows={resource.data?.orders || []}
      columns={[
        {
          key: "orderNumber",
          label: "Order",
          render: (r) => (
            <Link to={`/admin/orders/${r.id}`}>{r.orderNumber}</Link>
          ),
        },
        { key: "userName", label: "Customer" },
        {
          key: "createdAt",
          label: "Created",
          render: (r) => formatDateTime(r.createdAt),
        },
        {
          key: "total",
          label: "Total",
          render: (r) => checkoutMoney(r.total, r.currency),
        },
        { key: "status", label: "Order state" },
        { key: "paymentStatus", label: "Payment state" },
        { key: "fulfilmentStatus", label: "Fulfilment" },
      ]}
    />
  );
}
