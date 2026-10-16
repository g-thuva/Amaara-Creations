export function FilterBar({ children, onSubmit }) {
  return onSubmit ? (
    <form className="form-row" onSubmit={onSubmit}>
      {children}
    </form>
  ) : (
    <div className="form-row">{children}</div>
  );
}
