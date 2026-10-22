import { formatDateTime } from "@/features/admin/shared/utils/format.js";
import { DataTable } from "@/features/admin/shared/components/DataTable.jsx";
import { CopyableId, Disclosure } from '@/features/admin/shared/components/AdminUi.jsx';
export function AuditTable({ resource }) {
  return (
    <DataTable
      caption="Append-only audit evidence"
      rows={resource.data?.items || []}
      columns={[
        {
          key: "createdAt",
          label: "Time",
          render: (r) => <time title={r.createdAt} dateTime={r.createdAt}>{formatDateTime(r.createdAt)}</time>,
        },
        { key: "actorUserId", label: "Actor", render: r => <CopyableId value={r.actorUserId} label="actor identifier" /> },
        { key: "action", label: "Action", render: r => <span className="badge" title={r.action}>{r.action?.replace(/([a-z])([A-Z])/g, '$1 $2').replace(/[._]/g, ' ')}</span> },
        {
          key: "entityType",
          label: "Entity",
          render: (r) => <><span>{r.entityType}</span><br /><CopyableId value={r.entityId} label="entity identifier" /></>,
        },
        { key: "correlationId", label: "Correlation", render: r => <CopyableId value={r.correlationId} label="correlation identifier" /> },
        {
          key: "changes",
          label: "Changes",
          render: (r) => (
            <Disclosure>
              <p>Before</p>
              <pre>{r.oldValuesJson || "—"}</pre>
              <p>After</p>
              <pre>{r.newValuesJson || "—"}</pre>
            </Disclosure>
          ),
        },
      ]}
    />
  );
}
