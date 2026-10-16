import { useState } from "react";

// Each invocation owns an independent list (including independent pagination).
export function useAdminListParams(initial = {}) {
  const [params, setParams] = useState({ page: 1, ...initial });
  const setPage = (page) => setParams((current) => ({ ...current, page }));
  const setFilters = (filters) =>
    setParams((current) => ({ ...current, ...filters, page: 1 }));
  return { params, setPage, setFilters };
}
