import '../account.css';
import { accountNavigation } from '@/app/config/navigation.js';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';


export default function AccountLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();

  return (
    <div className="s-container s-page">
      <div className="s-account-heading">
        <div className="s-account-heading-copy">
          <p className="s-eyebrow">Your Amaara account</p>
          <h2>Welcome, {user?.name || 'back'}.</h2>
          <p>Manage your details, orders and saved creations in one place.</p>
        </div>
        <Link className="s-button s-button-secondary s-account-shop-link" to="/products">
          Continue shopping <Icon name="arrow" />
        </Link>
      </div>
      <div className="s-account-layout">
        <aside className="s-account-sidebar">
          <p className="s-account-nav-label">Account menu</p>
          <nav className="s-account-nav" aria-label="Account navigation">
            {accountNavigation.map(([to, icon, label]) => <NavLink key={to} to={to}><Icon name={icon} />{label}</NavLink>)}
            <Button variant="text" onClick={() => logout(() => navigate('/'))}><Icon name="logout" />Sign out</Button>
          </nav>
        </aside>
        <div className="s-account-content"><Outlet /></div>
      </div>
    </div>
  );
}
