import { FilterBar } from '@/features/admin/shared/components/FilterBar.jsx';
export function AuditFilters({ setFilters, setPage }) {
  return (
    <FilterBar
      onSubmit={(e) => {
        e.preventDefault();
        setFilters(
          Object.fromEntries(
            [...new FormData(e.currentTarget)].filter(([, v]) => v),
          ),
        );
        setPage(1);
      }}
    >
      {[
        ["action", "Action"],
        ["entityType", "Entity type"],
        ["entityId", "Entity identifier"],
      ].map(([key, label]) => (
        <label key={key}>
          {label}
          <input className="form-control" name={key} />
        </label>
      ))}
      <div className="action-buttons"><button className="btn btn-primary">Filter audit</button><button type="reset" className="btn btn-ghost" onClick={() => { setFilters({}); setPage(1); }}>Reset filters</button></div>
    </FilterBar>
  );
}
