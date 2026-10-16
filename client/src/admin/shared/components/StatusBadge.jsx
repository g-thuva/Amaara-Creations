export function StatusBadge({ status, tone = "warning", className = "" }) {
  return (
    <span className={`badge badge-${tone} ${className}`.trim()}>{status}</span>
  );
}
