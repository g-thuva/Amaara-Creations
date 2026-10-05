import { Link, NavLink, Outlet, useNavigate } from 'react-router-dom';
import { useAuth } from '../../contexts/AuthContext';
import { Button } from './UI';
export default function AccountLayout() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  return <div className="s-container s-page"><div className="s-account-heading"><p className="s-eyebrow">Your Amaara account</p><p>Welcome, {user?.name || 'back'}.</p></div><div className="s-account-layout"><aside><nav className="s-account-nav" aria-label="Account navigation">{[['/profile', 'Profile'], ['/addresses', 'Addresses'], ['/orders', 'Orders'], ['/wishlist', 'Wishlist'], ['/account/security', 'Security']].map(([to, label]) => <NavLink key={to} to={to}>{label}</NavLink>)}{user?.roles?.some(role => ['Admin', 'SuperAdmin'].includes(role)) && <Link to="/admin">Admin panel</Link>}<Button variant="text" onClick={async () => { await logout(); navigate('/'); }}>Sign out</Button></nav></aside><div className="s-account-content"><Outlet/></div></div></div>;
}
