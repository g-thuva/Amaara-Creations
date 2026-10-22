import { useProductionQueue } from "@/features/admin/features/production/useProductionQueue.js";
import { ProductionQueueTable } from "@/features/admin/features/production/ProductionQueueTable.jsx";
import { ProductionFilters } from "@/features/admin/features/production/ProductionFilters.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
import { PageHeader } from '@/features/admin/shared/components/PageHeader.jsx';
export default function ProductionQueuePage() {
  const { filters, setFilters, resource } = useProductionQueue();
  return (
    <div className="admin-page">
      <PageHeader title="Custom design production" description="Ordered custom stickers, proofs and production status." />
      <ProductionFilters filters={filters} setFilters={setFilters} />
      <ResourceState resource={resource}>
        <ProductionQueueTable resource={resource} />
        <Pagination
          page={filters.page}
          data={resource.data}
          setPage={(page) => setFilters({ ...filters, page })}
          total={resource.data?.totalCount || 0}
          size={20}
        />
      </ResourceState>
    </div>
  );
}
