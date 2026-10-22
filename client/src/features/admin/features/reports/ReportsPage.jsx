import { useReports } from "@/features/admin/features/reports/useReports.js";
import { ReportTables } from "@/features/admin/features/reports/ReportTables.jsx";
import { ReportDateFilters } from "@/features/admin/features/reports/ReportDateFilters.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import { MutationFeedback } from "@/features/admin/shared/components/MutationFeedback.jsx";
import { PageHeader, Callout } from '@/features/admin/shared/components/AdminUi.jsx';
export default function ReportsPage() {
  const reports = useReports();
  return (
    <div className="admin-page">
      <PageHeader title="Reports" description="Review store activity and download orders for the selected reporting period." />
      <Callout><p>
        Financial values remain separate by currency and payment environment.
        Order totals include unpaid orders; settled funds are shown separately.
      </p></Callout>
      <ReportDateFilters {...reports} />
      <MutationFeedback {...reports.mutation} />
      <ResourceState resource={reports.resource}>
        {reports.resource.data && <ReportTables resource={reports.resource} />}
      </ResourceState>
    </div>
  );
}
