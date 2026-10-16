export function SectionCard({ title, children, actions, inline = false }) {
  if (inline)
    return (
      <section className="card card-body">
        <h2>{title}</h2>
        {children}
      </section>
    );
  return (
    <section className="card">
      {title && (
        <div className="card-header">
          <h2 className="card-title">{title}</h2>
          {actions}
        </div>
      )}
      <div className="card-body">{children}</div>
    </section>
  );
}
