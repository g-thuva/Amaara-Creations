import { formatDateTime } from "../../shared/utils/formatDateTime.js";
import { DataTable } from "../../shared/components/DataTable.jsx";
export function AuditTable({ resource }) {
  return (
    <DataTable
      caption="Append-only audit evidence"
      rows={resource.data?.items || []}
      columns={[
        {
          key: "createdAt",
          label: "Time",
          render: (r) => formatDateTime(r.createdAt),
        },
        { key: "actorUserId", label: "Actor" },
        { key: "action", label: "Action" },
        {
          key: "entityType",
          label: "Entity",
          render: (r) => `${r.entityType} ${r.entityId || ""}`,
        },
        { key: "correlationId", label: "Correlation" },
        {
          key: "changes",
          label: "Changes",
          render: (r) => (
            <details>
              <summary>View safe metadata</summary>
              <p>Before</p>
              <pre>{r.oldValuesJson || "—"}</pre>
              <p>After</p>
              <pre>{r.newValuesJson || "—"}</pre>
            </details>
          ),
        },
      ]}
    />
  );
}
