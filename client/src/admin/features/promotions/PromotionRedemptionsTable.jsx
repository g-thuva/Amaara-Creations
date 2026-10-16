import { formatDateTime } from "../../shared/utils/formatDateTime.js";
import { DataTable } from "../../shared/components/DataTable.jsx";
import { checkoutMoney } from "../../../services/checkoutApi.js";
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
