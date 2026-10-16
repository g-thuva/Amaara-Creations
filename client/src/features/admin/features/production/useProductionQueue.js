import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { productionApi } from "@/features/admin/features/production/productionApi.js";
export function useProductionQueue() {
  const [filters, setFilters] = useState({
    search: "",
    designStatus: "Ordered",
    proofStatus: "",
    productionStatus: "",
    page: 1,
    pageSize: 20,
  });
  const resource = useResource(
    (signal) => productionApi.listJobs(filters, signal),
    JSON.stringify(filters),
  );
  return { filters, setFilters, resource };
}
