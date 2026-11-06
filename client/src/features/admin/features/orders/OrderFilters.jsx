import { FilterBar } from "@/features/admin/shared/components/FilterBar.jsx";
export function OrderFilters({ search, setSearch, status, setStatus, from = '', to = '', sort = 'newest', setFilters }) {
  return (
    <FilterBar>
      <label>
        Search order or customer
        <input
          className="form-control"
          value={search}
          onChange={(e) => {
            setSearch(e.target.value);
          }}
        />
      </label>
      <label>
        Order state
        <select
          className="form-control"
          value={status}
          onChange={(e) => {
            setStatus(e.target.value);
          }}
        >
          <option value="">All</option>
          {[
            "Pending",
            "PendingPayment",
            "Confirmed",
            "Processing",
            "Shipped",
            "Delivered",
            "Completed",
            "Cancelled",
            "PaymentReview",
          ].map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
      </label>
      <label>From date (UTC)<input className="form-control" type="date" value={from} max={to || undefined} onChange={e => setFilters({ from: e.target.value })} /></label>
      <label>To date (UTC)<input className="form-control" type="date" value={to} min={from || undefined} onChange={e => setFilters({ to: e.target.value })} /></label>
      <label>Sort orders<select className="form-control" value={sort} onChange={e => setFilters({ sort: e.target.value })}><option value="newest">Newest first</option><option value="oldest">Oldest first</option><option value="number">Order number</option></select></label>
    </FilterBar>
  );
}
