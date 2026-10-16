import { useState } from "react";
import { useResource } from "@/shared/hooks/useResource.js";
import { useAdminMutation } from "@/features/admin/shared/hooks/useAdminMutation.js";
import { downloadFile } from "@/shared/utils/downloadFile.js";
import { reportsApi } from "@/features/admin/features/reports/reportsApi.js";
export function useReports() {
  const [dates, setDates] = useState({});
  const resource = useResource(
    (signal) => reportsApi.getReports(dates, signal),
    JSON.stringify(dates),
  );
  const mutation = useAdminMutation();
  const exportOrders = () =>
    mutation.run(async () => {
      const blob = await reportsApi.exportOrders(dates);
      downloadFile(blob, "orders.csv");
    }, "Order export downloaded.");
  return { dates, setDates, resource, mutation, exportOrders };
}
