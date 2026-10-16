import { FilterBar } from "@/features/admin/shared/components/FilterBar.jsx";
export function OrderFilters({ search, setSearch, status, setStatus }) {
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
    </FilterBar>
  );
}
