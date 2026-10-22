import '@/features/auth/auth.css';
import Icon from '@/shared/components/ui/Icon.jsx';

export default function AuthShell({ children, security = false }) {
  return (
    <div className="auth-container">
      <aside className="auth-brand-panel">
        <div className="auth-sticker-motif" aria-hidden="true"><span /><span /><span /><span /></div>
        <p className="s-eyebrow">Amaara Creations</p>
        <h2>{security ? 'A little care, for everything you create.' : 'Your saved details, ready when you return.'}</h2>
        {security ? <p>One quick check keeps your account and the people who rely on it protected.</p> : <ul>
          <li><Icon name="heart" />Keep a wishlist</li>
          <li><Icon name="mapPin" />Manage delivery addresses</li>
          <li><Icon name="package" />Review your order history</li>
        </ul>}
      </aside>
      {children}
    </div>
  );
}
