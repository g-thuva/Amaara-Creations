import { useAdminListParams } from "@/features/admin/shared/hooks/useAdminListParams.js";
import { useResource } from "@/shared/hooks/useResource.js";
import { customersApi } from "@/features/admin/features/customers/customersApi.js";
export function useCustomers() {
  const {
    params: { page, search },
    setPage,
    setFilters,
  } = useAdminListParams({ search: "" });
  const setSearch = (search) => setFilters({ search });
  const resource = useResource(
    (signal) =>
      customersApi.getAllCustomers({ pageNumber: page, search }, signal),
    JSON.stringify([page, search]),
  );
  return { page, setPage, search, setSearch, resource };
}
