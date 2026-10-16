import { useOrders } from "@/features/admin/features/orders/useOrders.js";
import { OrdersTable } from "@/features/admin/features/orders/OrdersTable.jsx";
import { OrderFilters } from "@/features/admin/features/orders/OrderFilters.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
export default function OrdersPage() {
  const { page, setPage, search, setSearch, status, setStatus, resource } =
    useOrders();
  return (
    <div className="admin-page">
      <h1>Order management</h1>
      <OrderFilters
        search={search}
        setSearch={setSearch}
        status={status}
        setStatus={setStatus}
      />
      <ResourceState resource={resource}>
        <OrdersTable resource={resource} />
        <Pagination
          page={page}
          data={resource.data}
          setPage={setPage}
          total={resource.data?.totalCount || 0}
          size={20}
        />
      </ResourceState>
    </div>
  );
}
