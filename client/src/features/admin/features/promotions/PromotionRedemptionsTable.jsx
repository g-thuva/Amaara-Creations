import { formatDateTime } from "@/shared/utils/formatDate.ts";
import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
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
