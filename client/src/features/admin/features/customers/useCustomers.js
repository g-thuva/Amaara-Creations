import { useAdminListParams } from "@/features/admin/shared/hooks/useAdminListParams.js";
import { useResource } from "@/shared/hooks/useResource.js";
import { customersApi } from "@/features/admin/features/customers/customersApi.js";
export function useCustomers() {
  const {
    params: { page, search, status, sort },
    setPage,
    setFilters,
  } = useAdminListParams({ search: "", status: '', sort: 'newest' });
  const setSearch = (search) => setFilters({ search });
  const resource = useResource(
    (signal) =>
      customersApi.getAllCustomers({ pageNumber: page, search, status, sort }, signal),
    JSON.stringify([page, search, status, sort]),
  );
  return { page, setPage, search, setSearch, status, sort, setFilters, resource };
}
