export function PageHeader({ title, children, actions, description, eyebrow }) {
  return (
    <div className="page-header">
      <div className="header-content">
        <div>
          {eyebrow && <span className="page-eyebrow">{eyebrow}</span>}
          <h1>{title}</h1>
          {description && <p>{description}</p>}
          {children}
        </div>
        {actions && <div className="page-actions">{actions}</div>}
      </div>
    </div>
  );
}
