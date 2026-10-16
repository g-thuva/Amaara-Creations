import { useAudit } from "@/features/admin/features/audit/useAudit.js";
import { AuditTable } from "@/features/admin/features/audit/AuditTable.jsx";
import { AuditFilters } from "@/features/admin/features/audit/AuditFilters.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
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
