import { FilterBar } from '@/features/admin/shared/components/FilterBar.jsx';
export function ProductionFilters({ filters, setFilters }) {
  return (
    <FilterBar>
      <label>
        Search order or customer
        <input
          className="form-control"
          value={filters.search}
          onChange={(e) =>
            setFilters({ ...filters, search: e.target.value, page: 1 })
          }
        />
      </label>
      {[
        ["designStatus", "Design state", ["Ordered", "Archived"]],
        [
          "proofStatus",
          "Proof state",
          [
            "Preparing",
            "AwaitingApproval",
            "ChangesRequested",
            "Approved",
            "NotRequired",
          ],
        ],
        [
          "productionStatus",
          "Production state",
          [
            "NotStarted",
            "Queued",
            "Printing",
            "Finishing",
            "QualityCheck",
            "Ready",
          ],
        ],
      ].map(([key, label, choices]) => (
        <label key={key}>
          {label}
          <select
            className="form-control"
            value={filters[key]}
            onChange={(e) =>
              setFilters({ ...filters, [key]: e.target.value, page: 1 })
            }
          >
            <option value="">All</option>
            {choices.map((v) => (
              <option key={v}>{v}</option>
            ))}
          </select>
        </label>
      ))}
    </FilterBar>
  );
}
