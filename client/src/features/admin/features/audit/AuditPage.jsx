import { useAudit } from "@/features/admin/features/audit/useAudit.js";
import { AuditTable } from "@/features/admin/features/audit/AuditTable.jsx";
import { AuditFilters } from "@/features/admin/features/audit/AuditFilters.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
import { PageHeader } from '@/features/admin/shared/components/PageHeader.jsx';
import AppIcon from '@/shared/components/ui/Icon.jsx';
export default function AuditPage() {
  const { page, setPage, setFilters, resource } = useAudit();
  return (
    <div className="admin-page">
      <PageHeader title="Admin audit log" description="Review recorded staff activity and safe change metadata." eyebrow={<><AppIcon name="lock" />Append-only audit evidence</>} />
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
