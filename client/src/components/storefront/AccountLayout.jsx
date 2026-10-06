import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { Button, Icon } from './UI';

const links = [
  ['/profile', 'user', 'Profile'],
  ['/addresses', 'mapPin', 'Addresses'],
  ['/orders', 'package', 'Orders'],
  ['/account/designs', 'edit', 'Saved designs'],
  ['/wishlist', 'heart', 'Wishlist'],
  ['/account/security', 'lock', 'Security'],
];

export default function AccountLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const isAdmin = user?.roles?.some((role) => ['Admin', 'SuperAdmin'].includes(role));

  return (
    <div className="s-container s-page">
      <div className="s-account-heading">
        <p className="s-eyebrow">Your Amaara account</p>
        <p>Welcome, {user?.name || 'back'}.</p>
      </div>
      <div className="s-account-layout">
        <aside>
          <nav className="s-account-nav" aria-label="Account navigation">
            {links.map(([to, icon, label]) => <NavLink key={to} to={to}><Icon name={icon} />{label}</NavLink>)}
            {isAdmin && <Link to="/admin"><Icon name="settings" />Admin panel</Link>}
            <Button variant="text" onClick={async () => { await logout(); navigate('/'); }}><Icon name="logout" />Sign out</Button>
          </nav>
        </aside>
        <div className="s-account-content"><Outlet /></div>
      </div>
    </div>
  );
}
