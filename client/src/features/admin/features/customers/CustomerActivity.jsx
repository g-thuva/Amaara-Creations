import { formatLocalDate as formatDate } from "@/features/admin/shared/utils/format.js";
import { Link } from "react-router-dom";
import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
export function CustomerActivity({ data }) {
  return (
    <>
      <DataTable
        caption="Recent customer orders (up to 50)"
        rows={data.orders}
        columns={[
          {
            key: "orderNumber",
            label: "Order",
            render: (r) => (
              <Link to={`/admin/orders/${r.id}`}>{r.orderNumber}</Link>
            ),
          },
          {
            key: "orderDate",
            label: "Date",
            render: (r) => formatDate(r.orderDate),
          },
          { key: "status", label: "Status" },
          {
            key: "total",
            label: "Total",
            render: (r) => checkoutMoney(r.total, r.currency),
          },
        ]}
      />
      <DataTable
        caption="Recent custom designs (up to 50)"
        rows={data.designs}
        columns={[
          { key: "name", label: "Name" },
          { key: "status", label: "Design" },
          { key: "proofStatus", label: "Proof" },
          { key: "productionStatus", label: "Production" },
        ]}
      />
      <DataTable
        caption="Recent customer reviews (up to 50)"
        rows={data.reviews}
        columns={[
          { key: "productId", label: "Product" },
          { key: "rating", label: "Rating" },
          { key: "comment", label: "Review" },
          { key: "moderationStatus", label: "Moderation" },
        ]}
      />
    </>
  );
}
