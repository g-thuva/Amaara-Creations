import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
export function ReportTables({ resource }) {
  return (
    <>
      {" "}
      <DataTable
        caption="Settled funds by currency and environment"
        rows={resource.data.money}
        columns={[
          { key: "currency", label: "Currency" },
          {
            key: "providerMode",
            label: "Environment",
            render: (r) => r.providerMode || "Historical / unspecified",
          },
          { key: "attempts", label: "Attempts" },
          {
            key: "settled",
            label: "Settled",
            render: (r) => checkoutMoney(r.settled, r.currency),
          },
          {
            key: "refunded",
            label: "Refunded",
            render: (r) => checkoutMoney(r.refunded, r.currency),
          },
        ]}
      />
      <DataTable
        caption="Orders by status"
        rows={resource.data.orders}
        columns={[
          { key: "status", label: "State" },
          { key: "currency", label: "Currency" },
          { key: "count", label: "Count" },
          {
            key: "total",
            label: "Order total",
            render: (r) => checkoutMoney(r.total, r.currency),
          },
        ]}
      />
      <DataTable
        caption="Items on confirmed paid orders"
        rows={resource.data.products}
        columns={[
          { key: "product", label: "Snapshot product" },
          { key: "type", label: "Type" },
          { key: "currency", label: "Currency" },
          { key: "quantity", label: "Quantity" },
          {
            key: "total",
            label: "Line total",
            render: (r) => checkoutMoney(r.total, r.currency),
          },
        ]}
      />
      <DataTable
        caption="Current production backlog"
        rows={resource.data.production}
        columns={[
          { key: "state", label: "State" },
          { key: "count", label: "Jobs" },
        ]}
      />
      <DataTable
        caption="Inventory movements"
        rows={resource.data.inventory}
        columns={[
          { key: "type", label: "Type" },
          { key: "count", label: "Movements" },
          { key: "delta", label: "Available-stock delta" },
        ]}
      />
      <DataTable
        caption="Promotion usage"
        rows={resource.data.promotions}
        columns={[
          { key: "code", label: "Code" },
          { key: "currency", label: "Currency" },
          { key: "status", label: "State" },
          { key: "count", label: "Redemptions" },
          {
            key: "discount",
            label: "Discount",
            render: (r) => checkoutMoney(r.discount, r.currency),
          },
        ]}
      />
    </>
  );
}
