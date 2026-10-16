import { formatDateTime } from "../../shared/utils/formatDateTime.js";
import { DataTable } from "../../shared/components/DataTable.jsx";
import { checkoutMoney } from "../../../services/checkoutApi.js";
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
