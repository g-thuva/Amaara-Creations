import AppIcon from '@/shared/components/ui/Icon.jsx';
export function EmptyState({ children, title = 'No records found.', description, icon = 'layers', action }) {
  return <div className="admin-empty-state" role="status"><span className="admin-state-icon"><AppIcon name={icon} /></span><h2>{title}</h2>{(description || children) && <p>{description || children}</p>}{action}</div>;
}
