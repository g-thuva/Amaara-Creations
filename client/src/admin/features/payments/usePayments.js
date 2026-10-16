import { useAdminListParams } from "../../shared/hooks/useAdminListParams.js";
import { useResource } from "../../../hooks/useResource.js";
import { paymentsApi } from "./paymentsApi.js";
export function usePayments() {
  const {
    params: { page, status, mode },
    setPage,
    setFilters,
  } = useAdminListParams({ status: "", mode: "" });
  const setStatus = (status) => setFilters({ status });
  const setMode = (mode) => setFilters({ mode });
  const resource = useResource(
    (signal) =>
      paymentsApi.listPayments(
        { page, status: status || undefined, mode: mode || undefined },
        signal,
      ),
    JSON.stringify([page, status, mode]),
  );
  return { page, setPage, status, setStatus, mode, setMode, resource };
}
