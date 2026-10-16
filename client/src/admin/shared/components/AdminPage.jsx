export function AdminPage({ children, className = "" }) {
  return <div className={`admin-page ${className}`.trim()}>{children}</div>;
}
