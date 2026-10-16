import { DataTable } from "../../shared/components/DataTable.jsx";
import { checkoutMoney } from "../../../services/checkoutApi.js";
export function PromotionsTable({ resource, setEditing, setHistoryId }) {
  return (
    <DataTable
      caption="Promotion rules"
      rows={resource.data?.items || []}
      columns={[
        { key: "code", label: "Code" },
        { key: "name", label: "Name" },
        {
          key: "discountValue",
          label: "Discount",
          render: (r) =>
            r.discountType === 0
              ? `${r.discountValue}%`
              : checkoutMoney(r.discountValue, r.currency),
        },
        {
          key: "isActive",
          label: "State",
          render: (r) => (r.isActive ? "Active" : "Disabled"),
        },
        { key: "usageCount", label: "Committed uses" },
        { key: "usageLimit", label: "Limit" },
        {
          key: "actions",
          label: "Actions",
          render: (r) => (
            <>
              <button
                className="btn btn-outline-primary"
                onClick={() => setEditing(r)}
              >
                Edit {r.code}
              </button>
              <button
                className="btn btn-outline-primary"
                onClick={() => setHistoryId(r.id)}
              >
                History {r.code}
              </button>
            </>
          ),
        },
      ]}
    />
  );
}
