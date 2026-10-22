import AppIcon from '@/shared/components/ui/Icon.jsx';
import { FilterBar } from '@/features/admin/shared/components/FilterBar.jsx';
export function ReportDateFilters({
  setDates,
  mutation,
  resource,
  exportOrders,
}) {
  return (
    <div className="card card-body"><h2>Reporting period</h2><p className="admin-helper">Dates are interpreted in UTC. “Before” excludes the chosen date.</p><FilterBar
      onSubmit={(e) => {
        e.preventDefault();
        const f = new FormData(e.currentTarget);
        setDates({
          from: f.get("from") || undefined,
          to: f.get("to") || undefined,
        });
      }}
    >
      <label>
        From (UTC)
        <input className="form-control" type="date" name="from" />
      </label>
      <label>
        Before (UTC, exclusive)
        <input className="form-control" type="date" name="to" />
      </label>
      <div className="action-buttons"><button className="btn btn-primary">Apply dates</button>
      <button
        type="button"
        className="btn btn-outline-primary"
        disabled={
          mutation.pending || resource.loading || Boolean(resource.error)
        }
        onClick={exportOrders}
      >
        <AppIcon name="file" />Export orders CSV
      </button></div>
    </FilterBar></div>
  );
}
