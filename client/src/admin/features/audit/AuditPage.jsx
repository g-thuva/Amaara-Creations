import { useAudit } from "./useAudit.js";
import { AuditTable } from "./AuditTable.jsx";
import { AuditFilters } from "./AuditFilters.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { Pagination } from "../../shared/components/Pagination.jsx";
export default function AuditPage() {
  const { page, setPage, setFilters, resource } = useAudit();
  return (
    <div className="admin-page">
      <h1>Admin audit log</h1>
      <AuditFilters setFilters={setFilters} setPage={setPage} />
      <ResourceState resource={resource}>
        <AuditTable resource={resource} />
        <Pagination
          page={page}
          data={resource.data}
          setPage={setPage}
          total={resource.data?.totalCount || 0}
          size={50}
        />
      </ResourceState>
    </div>
  );
}
