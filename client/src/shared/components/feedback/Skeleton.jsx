export function Skeleton({ count = 4 }) {
  return (
    <div role="status" aria-label="Loading content" className="s-skeleton-grid">
      {Array.from({ length: count }, (_, index) => (
        <div key={index} className="s-skeleton" aria-hidden="true" />
      ))}
    </div>
  );
}
