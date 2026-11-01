export function Skeleton({ count = 4, variant = 'card', label = 'Loading content' }) {
  return (
    <div role="status" aria-label={label} className={`s-skeleton-grid s-skeleton-grid--${variant}`}>
      {Array.from({ length: count }, (_, index) => (
        <div key={index} className="s-skeleton" aria-hidden="true" />
      ))}
      <span className="sr-only">Loading…</span>
    </div>
  );
}
