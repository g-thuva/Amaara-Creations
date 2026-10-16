import { useState } from "react";
import { useResource } from "../../../hooks/useResource.js";
import { auditApi } from "./auditApi.js";
export function useAudit() {
  const [page, setPage] = useState(1);
  const [filters, setFilters] = useState({});
  const resource = useResource(
    (signal) => auditApi.listEntries({ ...filters, page }, signal),
    JSON.stringify([filters, page]),
  );
  return { page, setPage, filters, setFilters, resource };
}
