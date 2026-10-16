import { can } from '@/app/config/permissions.js';
import { accountNavigation } from '@/app/config/navigation.js';
import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { Button } from '@/shared/components/ui/Button.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';


export default function AccountLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const isAdmin = can(user, "AdminAccess");

  return (
    <div className="s-container s-page">
      <div className="s-account-heading">
        <p className="s-eyebrow">Your Amaara account</p>
        <p>Welcome, {user?.name || 'back'}.</p>
      </div>
      <div className="s-account-layout">
        <aside>
          <nav className="s-account-nav" aria-label="Account navigation">
            {accountNavigation.map(([to, icon, label]) => <NavLink key={to} to={to}><Icon name={icon} />{label}</NavLink>)}
            {isAdmin && <Link to="/admin"><Icon name="settings" />Admin panel</Link>}
            <Button variant="text" onClick={() => logout(() => navigate('/'))}><Icon name="logout" />Sign out</Button>
          </nav>
        </aside>
        <div className="s-account-content"><Outlet /></div>
      </div>
    </div>
  );
}
