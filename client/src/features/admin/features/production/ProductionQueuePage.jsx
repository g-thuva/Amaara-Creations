import { useProductionQueue } from "@/features/admin/features/production/useProductionQueue.js";
import { ProductionQueueTable } from "@/features/admin/features/production/ProductionQueueTable.jsx";
import { ProductionFilters } from "@/features/admin/features/production/ProductionFilters.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
export default function ProductionQueuePage() {
  const { filters, setFilters, resource } = useProductionQueue();
  return (
    <div className="admin-page">
      <h1>Custom design production</h1>
      <p>Ordered custom stickers, proofs and production status.</p>
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
