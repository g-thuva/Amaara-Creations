import { useAdminListParams } from "../../shared/hooks/useAdminListParams.js";
import { useResource } from "../../../hooks/useResource.js";
import { ordersApi } from "./ordersApi.js";
export function useOrders() {
  const {
    params: { page, search, status },
    setPage,
    setFilters,
  } = useAdminListParams({ search: "", status: "" });
  const setSearch = (search) => setFilters({ search });
  const setStatus = (status) => setFilters({ status });
  const resource = useResource(
    (signal) =>
      ordersApi.listOrders(
        { pageNumber: page, search, status: status || undefined },
        signal,
      ),
    JSON.stringify([page, search, status]),
  );
  return { page, setPage, search, setSearch, status, setStatus, resource };
}
