import { Link } from 'react-router-dom';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';

export default function NotFound() {
  return (
    <div className="s-container s-page s-not-found">
      <h1>Page not found</h1>
      <p className="s-eyebrow">Error 404</p>
      <EmptyState title="That page could not be found" to="/" label="Back to home">
        <p>Use the main navigation or return to the product catalogue.</p>
        <Link className="s-text-link" to="/products">Browse products <Icon name="arrow" /></Link>
      </EmptyState>
    </div>
  );
}
