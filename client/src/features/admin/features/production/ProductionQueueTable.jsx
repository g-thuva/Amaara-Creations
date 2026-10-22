import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
import { Link } from "react-router-dom";
export function ProductionQueueTable({ resource }) {
  return (
    <DataTable
      caption="Ordered production jobs"
      rows={resource.data?.items || []}
      columns={[
        {
          key: "name",
          label: "Design",
          render: (r) => (
            <Link to={`/admin/custom-designs/${r.id}`}>{r.name}</Link>
          ),
        },
        { key: "orderNumber", label: "Order" },
        { key: "customerName", label: "Customer" },
        {
          key: "spec",
          label: "Specification",
          render: (r) => `${r.width} × ${r.height} cm · ${r.quantity}`,
        },
        { key: "proofStatus", label: "Proof" },
        { key: "productionStatus", label: "Production" },
        {
          key: "calculatedPrice",
          label: "Subtotal",
          render: (r) => checkoutMoney(r.calculatedPrice, r.currency),
        },
      ]}
    />
  );
}
