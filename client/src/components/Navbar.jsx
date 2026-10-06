import { useEffect, useRef, useState } from 'react';
import { Link, NavLink, useNavigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';
import { useStore } from '../contexts/StoreContext';
import { Button, Dialog, Icon } from './storefront/UI';
import { safeLink } from '../utils/storefront';

const links = [
  ['/', 'Home'],
  ['/products', 'Shop'],
  ['/custom', 'Custom stickers'],
  ['/about', 'About'],
  ['/contact', 'Contact'],
];

function Brand({ name }) {
  return (
    <Link to="/" className="s-brand" aria-label={`${name} home`}>
      <svg className="s-brand-logo" viewBox="0 0 48 48" aria-hidden="true">
        <rect width="48" height="48" rx="13" />
        <path d="M13 35 22.4 12h4.2L36 35h-5.7l-1.7-4.8h-8.4L18.5 35H13Zm9-9.8h4.8l-2.4-7-2.4 7Z" />
        <circle cx="37" cy="11" r="4" />
      </svg>
      <span>Amaara <strong>Creations</strong></span>
    </Link>
  );
}

function AccountLinks({ close, signOut, isAdmin }) {
  return (
    <>
      <Link to="/profile" onClick={close}><Icon name="user" />Profile</Link>
      <Link to="/orders" onClick={close}><Icon name="package" />Orders</Link>
      <Link to="/addresses" onClick={close}><Icon name="mapPin" />Addresses</Link>
      <Link to="/wishlist" onClick={close}><Icon name="heart" />Wishlist</Link>
      <Link to="/account/security" onClick={close}><Icon name="lock" />Security</Link>
      {isAdmin && <Link to="/admin" onClick={close}><Icon name="settings" />Admin panel</Link>}
      <button type="button" onClick={signOut}><Icon name="logout" />Sign out</button>
    </>
  );
}

export default function Navbar() {
  const [open, setOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [scrolled, setScrolled] = useState(false);
  const { isAuthenticated, authStatus, user, logout } = useAuth();
  const { values, cart, wishlist } = useStore();
  const navigate = useNavigate();
  const sentinelRef = useRef(null);
  const searchRef = useRef(null);
  const announcement = values['announcement.text'];
  const announcementUrl = safeLink(values['announcement.url']);
  const dismissalKey = announcement ? `amaara-announcement:${announcement}` : '';
  const [announcementDismissed, setAnnouncementDismissed] = useState(
    () => typeof sessionStorage !== 'undefined' && dismissalKey && sessionStorage.getItem(dismissalKey) === 'dismissed',
  );
  const isAdmin = user?.roles?.some((role) => ['Admin', 'SuperAdmin'].includes(role));

  useEffect(() => {
    const sentinel = sentinelRef.current;
    if (!sentinel) return undefined;
    const observer = new IntersectionObserver(([entry]) => setScrolled(!entry.isIntersecting));
    observer.observe(sentinel);
    return () => observer.disconnect();
  }, []);

  useEffect(() => {
    if (searchOpen) searchRef.current?.focus();
  }, [searchOpen]);

  const signOut = async () => {
    setOpen(false);
    await logout();
    navigate('/');
  };

  const submitSearch = (event) => {
    event.preventDefault();
    const value = new FormData(event.currentTarget).get('search')?.toString().trim();
    navigate(value ? `/products?search=${encodeURIComponent(value)}` : '/products');
    setSearchOpen(false);
    setOpen(false);
  };

  const dismissAnnouncement = () => {
    if (dismissalKey && typeof sessionStorage !== 'undefined') sessionStorage.setItem(dismissalKey, 'dismissed');
    setAnnouncementDismissed(true);
  };

  return (
    <>
      <a
        className="s-skip"
        href="#store-main"
        onClick={(event) => {
          event.preventDefault();
          document.getElementById('store-main')?.focus();
        }}
      >
        Skip to content
      </a>

      {announcement && !announcementDismissed && (
        <div className="s-announcement">
          <div className="s-container">
            {announcementUrl ? (
              announcementUrl.startsWith('/') ? <Link to={announcementUrl}>{announcement}</Link> : <a href={announcementUrl} target="_blank" rel="noopener noreferrer">{announcement}</a>
            ) : <span>{announcement}</span>}
            <button type="button" aria-label="Dismiss announcement" onClick={dismissAnnouncement}><Icon name="x" /></button>
          </div>
        </div>
      )}

      <span ref={sentinelRef} className="s-header-sentinel" aria-hidden="true" />
      <header className={`s-header ${scrolled ? 'is-scrolled' : ''}`}>
        <div className="s-container s-header-inner">
          <Brand name={values['site.name'] || 'Amaara Creations'} />

          <nav className="s-desktop-nav" aria-label="Main navigation">
            {links.map(([to, label]) => <NavLink key={to} to={to}>{label}</NavLink>)}
          </nav>

          <div className="s-header-actions">
            <button
              type="button"
              className="s-icon-link s-search-toggle"
              aria-label={searchOpen ? 'Close product search' : 'Search products'}
              aria-expanded={searchOpen}
              onClick={() => setSearchOpen((value) => !value)}
            >
              <Icon name={searchOpen ? 'x' : 'search'} />
            </button>
            <Link className="s-icon-link s-wishlist-link" to="/wishlist" aria-label={`Wishlist${wishlist.data ? `, ${wishlist.data.totalItems} items` : ''}`}>
              <Icon name="heart" />
              {wishlist.data?.totalItems > 0 && <span className="s-count">{wishlist.data.totalItems}</span>}
            </Link>
            <details className="s-account-menu">
              <summary className="s-icon-link" aria-label={isAuthenticated ? 'Open account menu' : 'Open sign-in menu'}><Icon name="user" /></summary>
              <div className="s-account-popover">
                {authStatus === 'loading' ? <span>Restoring session…</span> : isAuthenticated ? (
                  <AccountLinks close={() => {}} signOut={signOut} isAdmin={isAdmin} />
                ) : (
                  <><Link to="/login">Sign in</Link><Link to="/register">Create an account</Link></>
                )}
              </div>
            </details>
            <Link className="s-icon-link" to="/cart" aria-label={`Cart${cart.data ? `, ${cart.data.totalItems} items` : ''}`}>
              <Icon name="bag" />
              {cart.data?.totalItems > 0 && <span className="s-count">{cart.data.totalItems}</span>}
            </Link>
            <button className="s-icon-link s-menu-button" type="button" onClick={() => setOpen(true)} aria-label="Open navigation" aria-expanded={open}>
              <Icon name="menu" />
            </button>
          </div>
        </div>

        {searchOpen && (
          <form className="s-header-search s-container" role="search" onSubmit={submitSearch}>
            <label className="sr-only" htmlFor="header-product-search">Search products</label>
            <Icon name="search" />
            <input ref={searchRef} id="header-product-search" type="search" name="search" placeholder="Search stickers, decals and prints" />
            <Button type="submit">Search</Button>
          </form>
        )}
      </header>

      <Dialog open={open} onClose={() => setOpen(false)} title="Explore Amaara" drawer>
        <nav className="s-mobile-nav" aria-label="Mobile navigation">
          {links.map(([to, label]) => (
            <NavLink key={to} to={to} onClick={() => setOpen(false)}>{label}<Icon name="arrow" /></NavLink>
          ))}
          <Link className="s-mobile-search" to="/products" onClick={() => setOpen(false)}><Icon name="search" />Search products</Link>
          {authStatus === 'loading' ? <p>Restoring session…</p> : isAuthenticated ? (
            <div className="s-mobile-account">
              <p>Your account</p>
              <AccountLinks close={() => setOpen(false)} signOut={signOut} isAdmin={isAdmin} />
            </div>
          ) : (
            <div className="s-mobile-account">
              <Link to="/login" onClick={() => setOpen(false)}>Sign in</Link>
              <Link to="/register" onClick={() => setOpen(false)}>Create an account</Link>
            </div>
          )}
          <Link className="s-button s-button-primary" to="/custom" onClick={() => setOpen(false)}>Design custom stickers</Link>
        </nav>
      </Dialog>
    </>
  );
}
