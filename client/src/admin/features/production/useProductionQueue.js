import { useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { productionApi } from "./productionApi.js";
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
