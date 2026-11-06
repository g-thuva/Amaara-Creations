import { useAdminListParams } from "@/features/admin/shared/hooks/useAdminListParams.js";
import { useResource } from "@/shared/hooks/useResource.js";
import { ordersApi } from "@/features/admin/features/orders/ordersApi.js";
export function useOrders() {
  const {
    params: { page, search, status, from, to, sort },
    setPage,
    setFilters,
  } = useAdminListParams({ search: "", status: "", from: '', to: '', sort: 'newest' });
  const setSearch = (search) => setFilters({ search });
  const setStatus = (status) => setFilters({ status });
  const resource = useResource(
    (signal) =>
      ordersApi.listOrders(
        { pageNumber: page, search, status: status || undefined, from: from || undefined, to: to || undefined, sort },
        signal,
      ),
    JSON.stringify([page, search, status, from, to, sort]),
  );
  return { page, setPage, search, setSearch, status, setStatus, from, to, sort, setFilters, resource };
}
