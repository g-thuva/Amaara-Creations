import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/features/admin/shared/utils/format.js';
import { StatusChip } from '@/features/admin/shared/components/StatusChip.jsx';
import { RowActions } from '@/features/admin/shared/components/RowActions.jsx';
export function PromotionsTable({ resource, setEditing, setHistoryId, create }) {
  return (
    <DataTable
      caption="Promotion rules"
      rows={resource.data?.items || []}
      empty="No promotions yet"
      emptyDescription="Create an offer when you are ready. New offers keep the activation state you choose."
      emptyAction={create && <button className="btn btn-outline-primary" onClick={create}>Create your first promotion</button>}
      columns={[
        { key: "code", label: "Code", render: r => <code className="admin-code">{r.code}</code> },
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
          render: (r) => <StatusChip status={r.isActive ? 'Active' : 'Disabled'} />,
        },
        { key: "usageCount", label: "Committed uses", render: r => <div>{r.usageCount}{r.usageLimit > 0 && <><br /><progress aria-label={r.code + ' committed uses'} value={r.usageCount} max={r.usageLimit} /></>}</div> },
        { key: "usageLimit", label: "Limit" },
        {
          key: "actions",
          label: "Actions",
          render: (r) => (
            <RowActions label={`Actions for ${r.code}`}>
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
            </RowActions>
          ),
        },
      ]}
    />
  );
}
