export function PageHeader({ title, children, actions }) {
  return (
    <div className="page-header">
      <div className="header-content">
        <div>
          <h1>{title}</h1>
          {children}
        </div>
        {actions}
      </div>
    </div>
  );
}
