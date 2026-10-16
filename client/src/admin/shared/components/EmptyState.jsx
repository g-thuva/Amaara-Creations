export function EmptyState({ children = "No records found." }) {
  return <p role="status">{children}</p>;
}
