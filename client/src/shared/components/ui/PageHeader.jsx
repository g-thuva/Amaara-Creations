export function PageHeader({ eyebrow, title, children, className = "" }) {
  return (
    <header className={`s-page-header ${className}`.trim()}>
      {eyebrow && <p className="s-eyebrow">{eyebrow}</p>}
      <h1>{title}</h1>
      <span className="s-title-accent" aria-hidden="true" />
      {children && <div className="s-lead">{children}</div>}
    </header>
  );
}
