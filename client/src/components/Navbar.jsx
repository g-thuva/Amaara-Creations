import { useState } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useStore } from '../contexts/StoreContext';
import { Button, Dialog, Icon } from './storefront/UI';
export default function Navbar() {
  const [open, setOpen] = useState(false);
  const { isAuthenticated, authStatus, user, logout } = useAuth();
  const { values, cart, wishlist } = useStore();
  const navigate = useNavigate();
  const links = [['/products', 'Shop all'], ['/custom', 'Custom stickers'], ['/about', 'Our story'], ['/contact', 'Contact']];
  const signOut = async () => { setOpen(false); await logout(); navigate('/'); };
  return <>
    <a className="s-skip" href="#store-main" onClick={e => { e.preventDefault(); document.getElementById('store-main')?.focus(); }}>Skip to content</a>
    {values['announcement.text'] && <div className="s-announcement">{values['announcement.text']}</div>}
    <header className="s-header"><div className="s-container s-header-inner">
      <Link to="/" className="s-brand" aria-label={`${values['site.name'] || 'Amaara Creations'} home`}><span className="s-brand-mark" aria-hidden="true">a<span>✳</span></span><span>{values['site.name'] || 'Amaara'}<small>{values['site.name'] ? '' : 'CREATIONS'}</small></span></Link>
      <nav className="s-desktop-nav" aria-label="Main navigation">{links.map(([to, label]) => <NavLink key={to} to={to}>{label}</NavLink>)}</nav>
      <div className="s-header-actions">
        <Link to="/products" className="s-icon-link s-search-link" aria-label="Search products"><Icon name="search"/></Link>
        <Link to="/wishlist" className="s-icon-link" aria-label={`Wishlist${wishlist.data ? `, ${wishlist.data.totalItems} items` : ''}`}><Icon name="heart"/>{wishlist.data?.totalItems > 0 && <span className="s-count">{wishlist.data.totalItems}</span>}</Link>
        <Link to={isAuthenticated ? '/profile' : '/login'} className="s-icon-link s-account-link" aria-label={isAuthenticated ? 'My account' : 'Sign in'}><Icon name="user"/></Link>
        <Link to="/cart" className="s-icon-link" aria-label={`Cart${cart.data ? `, ${cart.data.totalItems} items` : ''}`}><Icon name="bag"/>{cart.data?.totalItems > 0 && <span className="s-count">{cart.data.totalItems}</span>}</Link>
        <button className="s-icon-link s-menu-button" onClick={() => setOpen(true)} aria-label="Open navigation" aria-expanded={open}><Icon name="menu"/></button>
      </div>
    </div></header>
    <Dialog open={open} onClose={() => setOpen(false)} title="Explore Amaara" drawer><nav className="s-mobile-nav" aria-label="Mobile navigation">{links.map(([to, label]) => <NavLink key={to} to={to} onClick={() => setOpen(false)}>{label}<Icon name="arrow"/></NavLink>)}
      {authStatus === 'loading' ? <p>Restoring session…</p> : isAuthenticated ? <><Link to="/profile" onClick={() => setOpen(false)}>My account</Link><Link to="/orders" onClick={() => setOpen(false)}>My orders</Link>{user?.roles?.some(role => ['Admin', 'SuperAdmin'].includes(role)) && <Link to="/admin" onClick={() => setOpen(false)}>Admin panel</Link>}<Button variant="secondary" onClick={signOut}>Sign out</Button></> : <><Link to="/login" onClick={() => setOpen(false)}>Sign in</Link><Link to="/register" onClick={() => setOpen(false)}>Create an account</Link></>}
    </nav></Dialog>
  </>;
}
