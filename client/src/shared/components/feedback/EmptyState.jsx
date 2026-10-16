import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';
export function EmptyState({
  title,
  children,
  to,
  label = "Continue",
}) {
  return (
    <div className="s-empty">
      <span className="s-empty-symbol" aria-hidden="true">
        <Icon name="package" />
      </span>
      <h2>{title}</h2>
      {children && <div className="s-empty-copy">{children}</div>}
      {to && (
        <Link className="s-button s-button-primary" to={to}>
          {label}
          <Icon name="arrow" />
        </Link>
      )}
    </div>
  );
}
