import { useAdminListParams } from "../../shared/hooks/useAdminListParams.js";
import { useResource } from "../../../hooks/useResource.js";
import { customersApi } from "./customersApi.js";
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
