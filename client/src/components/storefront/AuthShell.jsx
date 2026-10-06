import { Icon } from './UI';

export default function AuthShell({ children }) {
  return (
    <div className="auth-container">
      <aside className="auth-brand-panel">
        <div className="auth-sticker-motif" aria-hidden="true"><span /><span /><span /><span /></div>
        <p className="s-eyebrow">Amaara Creations</p>
        <h2>Your saved details, ready when you return.</h2>
        <ul>
          <li><Icon name="heart" />Keep a wishlist</li>
          <li><Icon name="mapPin" />Manage delivery addresses</li>
          <li><Icon name="package" />Review your order history</li>
        </ul>
      </aside>
      {children}
    </div>
  );
}
