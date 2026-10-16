import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';
export function SectionHeading({
  title,
  to,
  label = "Explore all",
  align = "center",
}) {
  return (
    <div className={`s-section-heading s-section-heading-${align}`}>
      <div>
        <h2>{title}</h2>
        <span className="s-title-accent" aria-hidden="true" />
      </div>
      {to && (
        <Link to={to}>
          {label}
          <Icon name="arrow" />
        </Link>
      )}
    </div>
  );
}
