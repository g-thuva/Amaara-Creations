export function Skeleton({ rows = 4, label = 'Loading details…' }) {
  return <div className="admin-skeleton-group" role="status" aria-label={label}><span className="admin-sr-only">{label}</span>{Array.from({ length: rows }, (_, index) => <span className="admin-skeleton" aria-hidden="true" key={index} />)}</div>;
}
