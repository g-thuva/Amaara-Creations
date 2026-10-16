import { FilterBar } from "../../shared/components/FilterBar.jsx";
export function PaymentFilters({ status, setStatus, mode, setMode }) {
  return (
    <FilterBar>
      <label>
        Payment state
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
            "Authorized",
            "Paid",
            "Failed",
            "Cancelled",
            "PartiallyRefunded",
            "Refunded",
          ].map((s) => (
            <option key={s}>{s}</option>
          ))}
        </select>
      </label>
      <label>
        Environment
        <select
          className="form-control"
          value={mode}
          onChange={(e) => {
            setMode(e.target.value);
          }}
        >
          <option value="">All (including historical)</option>
          <option>TEST</option>
          <option>LIVE</option>
        </select>
      </label>
    </FilterBar>
  );
}
