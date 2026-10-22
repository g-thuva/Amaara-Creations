import { formatDateTime } from "@/features/admin/shared/utils/format.js";
import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
export function PromotionRedemptionsTable({ history }) {
  return (
    <DataTable
      caption="Promotion redemption history (latest 50)"
      rows={history.data?.items || []}
      columns={[
        { key: "orderId", label: "Order" },
        { key: "status", label: "State" },
        {
          key: "amount",
          label: "Discount",
          render: (r) => checkoutMoney(r.amount, r.currency),
        },
        {
          key: "createdAt",
          label: "Time",
          render: (r) => formatDateTime(r.createdAt),
        },
      ]}
    />
  );
}
