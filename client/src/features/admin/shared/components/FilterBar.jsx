export function FilterBar({ children, onSubmit }) {
  return onSubmit ? (
    <form className="admin-filter-bar" onSubmit={onSubmit}>
      {children}
    </form>
  ) : (
    <div className="admin-filter-bar">{children}</div>
  );
}
