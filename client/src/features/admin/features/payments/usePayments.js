import { useAdminListParams } from "@/features/admin/shared/hooks/useAdminListParams.js";
import { useResource } from "@/shared/hooks/useResource.js";
import { paymentsApi } from "@/features/admin/features/payments/paymentsApi.js";
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
