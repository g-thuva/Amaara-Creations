import { formatDateTime } from "@/shared/utils/formatDate.ts";
import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
export function InventoryMovementsTable({ movements }) {
  return (
    <DataTable
      caption="Available-stock movement history"
      rows={movements.data?.items || []}
      columns={[
        {
          key: "createdAt",
          label: "Time",
          render: (r) => formatDateTime(r.createdAt),
        },
        { key: "movementType", label: "Type" },
        { key: "quantityDelta", label: "Delta" },
        { key: "newQuantity", label: "Available after" },
        { key: "orderId", label: "Order" },
        { key: "reason", label: "Reason" },
        { key: "performedByUserId", label: "Actor" },
      ]}
    />
  );
}
