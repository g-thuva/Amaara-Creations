export function ReportDateFilters({
  setDates,
  mutation,
  resource,
  exportOrders,
}) {
  return (
    <form
      className="form-row"
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
      <button className="btn btn-primary">Apply dates</button>
      <button
        type="button"
        className="btn btn-outline-primary"
        disabled={
          mutation.pending || resource.loading || Boolean(resource.error)
        }
        onClick={exportOrders}
      >
        Export orders CSV
      </button>
    </form>
  );
}
