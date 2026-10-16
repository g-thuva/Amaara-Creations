export function AuditFilters({ setFilters, setPage }) {
  return (
    <form
      className="form-row"
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
      <button className="btn btn-primary">Filter audit</button>
    </form>
  );
}
