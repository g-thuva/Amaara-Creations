import { useOrders } from "./useOrders.js";
import { OrdersTable } from "./OrdersTable.jsx";
import { OrderFilters } from "./OrderFilters.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { Pagination } from "../../shared/components/Pagination.jsx";
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
