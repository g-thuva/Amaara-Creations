import { useReports } from "./useReports.js";
import { ReportTables } from "./ReportTables.jsx";
import { ReportDateFilters } from "./ReportDateFilters.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import { MutationFeedback } from "../../shared/components/MutationFeedback.jsx";
export default function ReportsPage() {
  const reports = useReports();
  return (
    <div className="admin-page">
      <h1>Reports</h1>
      <p>
        Financial values remain separate by currency and payment environment.
        Order totals include unpaid orders; settled funds are shown separately.
      </p>
      <ReportDateFilters {...reports} />
      <MutationFeedback {...reports.mutation} />
      <ResourceState resource={reports.resource}>
        {reports.resource.data && <ReportTables resource={reports.resource} />}
      </ResourceState>
    </div>
  );
}
