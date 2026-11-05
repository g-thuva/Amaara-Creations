import { storefrontNavigation } from '@/app/config/navigation.js';
import { useCart } from '@/features/cart/CartProvider.jsx';
import { useWishlist } from '@/features/wishlist/WishlistProvider.jsx';
import { useSiteContent } from '@/features/content/SiteContentProvider.jsx';
import { lazy, Suspense, useEffect, useRef, useState } from 'react';
import { Link, NavLink, useNavigate, useLocation } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { Button } from '@/shared/components/ui/Button.jsx';
import { BrandLogo } from '@/shared/components/ui/BrandLogo.jsx';
import { Dialog } from '@/shared/components/ui/Dialog.jsx';
import Icon from '@/shared/components/ui/Icon.jsx';
import { safeLink } from '@/shared/utils/safeLink.ts';

const CartDrawer = lazy(() => import('@/features/cart/components/CartDrawer.jsx'));

function Brand({ name }) {
  return (
    <Link to="/" className="s-brand" aria-label={`${name} home`}>
      <BrandLogo variant="lockup" tone="blue" className="s-header-brand" />
      <span className="sr-only">{name}</span>
    </Link>
  );
}

function AccountLinks({ close, signOut }) {
  return (
    <>
      <Link to="/profile" onClick={close}><Icon name="user" />Profile</Link>
      <Link to="/orders" onClick={close}><Icon name="package" />Orders</Link>
      <Link to="/addresses" onClick={close}><Icon name="mapPin" />Addresses</Link>
      <Link to="/wishlist" onClick={close}><Icon name="heart" />Wishlist</Link>
      <Link to="/account/security" onClick={close}><Icon name="lock" />Security</Link>
      <Link to="/account/designs" onClick={close}><Icon name="edit" />Saved designs</Link>
      <button type="button" onClick={signOut}><Icon name="logout" />Sign out</button>
    </>
  );
}

export default function Navbar() {
  const [open, setOpen] = useState(false);
  const [searchOpen, setSearchOpen] = useState(false);
  const [cartOpen, setCartOpen] = useState(false);
  const [cartUsed, setCartUsed] = useState(false);
  const [scrolled, setScrolled] = useState(false);
  const { isAuthenticated, authStatus, logout } = useAuth();
  const { cart } = useCart();
  const { wishlist } = useWishlist();
  const { values } = useSiteContent();
  const navigate = useNavigate();
  const location = useLocation();
  const sentinelRef = useRef(null);
  const searchRef = useRef(null);
  const searchFormRef = useRef(null);
  const searchToggleRef = useRef(null);
  const accountMenuRef = useRef(null);

  const closeAccountMenu = () => {
    accountMenuRef.current?.removeAttribute('open');
  };

  const isHome =
    location.pathname === '/' ||
    location.pathname === '' ||
    location.pathname === '/Amaara-Creations/' ||
    location.pathname === '/Amaara-Creations';

  const announcement = values['announcement.text'];
  const announcementUrl = safeLink(values['announcement.url']);
  const dismissalKey = announcement ? `amaara-announcement:${announcement}` : '';
  const [announcementDismissed, setAnnouncementDismissed] = useState(
    () => typeof sessionStorage !== 'undefined' && dismissalKey && sessionStorage.getItem(dismissalKey) === 'dismissed',
  );

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

  useEffect(() => {
    setSearchOpen(false);
    setOpen(false);
    setCartOpen(false);
    closeAccountMenu();
  }, [location.pathname, location.search]);

  useEffect(() => {
    const handlePointerDownOutside = (event) => {
      const target = event.target;
      if (accountMenuRef.current?.hasAttribute('open') && !accountMenuRef.current.contains(target)) {
        closeAccountMenu();
      }
      if (
        searchFormRef.current &&
        !searchFormRef.current.contains(target) &&
        !searchToggleRef.current?.contains(target)
      ) {
        setSearchOpen(false);
      }
    };

    const handleKeyDown = (event) => {
      if (event.key === 'Escape') {
        closeAccountMenu();
        setSearchOpen(false);
      }
    };

    document.addEventListener('mousedown', handlePointerDownOutside);
    document.addEventListener('touchstart', handlePointerDownOutside, { passive: true });
    document.addEventListener('keydown', handleKeyDown);
    return () => {
      document.removeEventListener('mousedown', handlePointerDownOutside);
      document.removeEventListener('touchstart', handlePointerDownOutside);
      document.removeEventListener('keydown', handleKeyDown);
    };
  }, []);

  const signOut = async () => {
    closeAccountMenu();
    setOpen(false);
    await logout(() => navigate('/'));
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
      <header className={`s-header ${scrolled || !isHome ? 'is-scrolled' : 'is-transparent'} ${isHome ? 'is-home' : ''}`}>
        <div className="s-container s-header-inner">
          <Brand name={values['site.name'] || 'Amaara Creations'} />

          <nav className="s-desktop-nav" aria-label="Main navigation">
            {storefrontNavigation.map(([to, label]) => (
              <NavLink key={to} to={to} end={to === '/'}>
                {label}
              </NavLink>
            ))}
          </nav>

          <div className="s-header-actions">
            <button
              ref={searchToggleRef}
              type="button"
              className="s-icon-link s-search-toggle"
              aria-label={searchOpen ? 'Close product search' : 'Search products'}
              aria-expanded={searchOpen}
              onClick={() => {
                closeAccountMenu();
                setSearchOpen((value) => !value);
              }}
            >
              <Icon name={searchOpen ? 'x' : 'search'} />
            </button>
            <Link
              className="s-icon-link s-wishlist-link"
              to="/wishlist"
              aria-label={`Wishlist${wishlist.data ? `, ${wishlist.data.totalItems} items` : ''}`}
            >
              <Icon name="heart" />
              {wishlist.data?.totalItems > 0 && <span className="s-count">{wishlist.data.totalItems}</span>}
            </Link>
            <details
              ref={accountMenuRef}
              className="s-account-menu"
              onToggle={(event) => {
                if (event.currentTarget.open) {
                  setSearchOpen(false);
                }
              }}
            >
              <summary className="s-icon-link" aria-label={isAuthenticated ? 'Open account menu' : 'Open sign-in menu'}>
                <Icon name="user" />
              </summary>
              <div className="s-account-popover">
                {authStatus === 'loading' ? (
                  <span>Restoring session…</span>
                ) : isAuthenticated ? (
                  <AccountLinks
                    close={closeAccountMenu}
                    signOut={signOut}
                  />
                ) : (
                  <>
                    <Link to="/login" onClick={closeAccountMenu}>Sign in</Link>
                    <Link to="/register" onClick={closeAccountMenu}>Create an account</Link>
                  </>
                )}
              </div>
            </details>
            <Link
              className="s-icon-link"
              to="/cart"
              aria-haspopup="dialog"
              aria-expanded={cartOpen}
              aria-controls={cartUsed ? 'cart-preview' : undefined}
              onClick={(event) => {
                if (event.ctrlKey || event.metaKey || event.shiftKey || event.altKey) return;
                event.preventDefault();
                closeAccountMenu();
                setSearchOpen(false);
                setCartUsed(true);
                setCartOpen(true);
              }}
              aria-label={`Cart${cart.data ? `, ${cart.data.totalItems} ${cart.data.totalItems === 1 ? 'item' : 'items'}` : ''}`}
            >
              <Icon name="bag" />
              {cart.data?.totalItems > 0 && <span className="s-count">{cart.data.totalItems}</span>}
            </Link>

            <Link className="s-button s-button-primary s-header-cta" to="/custom">
              Design Studio
            </Link>

            <button
              className="s-icon-link s-menu-button"
              type="button"
              onClick={() => {
                closeAccountMenu();
                setSearchOpen(false);
                setOpen(true);
              }}
              aria-label="Open navigation"
              aria-expanded={open}
            >
              <Icon name="menu" />
            </button>
          </div>
        </div>

        {searchOpen && (
          <form ref={searchFormRef} className="s-header-search s-container" role="search" onSubmit={submitSearch}>
            <label className="sr-only" htmlFor="header-product-search">Search products</label>
            <Icon name="search" />
            <input
              ref={searchRef}
              id="header-product-search"
              type="search"
              name="search"
              placeholder="Search welcome signs, stickers and prints"
            />
            <Button type="submit">Search</Button>
          </form>
        )}
      </header>

      {cartUsed && (
        <Suspense fallback={<p role="status">Loading cart preview…</p>}>
          <CartDrawer open={cartOpen} onClose={() => setCartOpen(false)} />
        </Suspense>
      )}

      <Dialog open={open} onClose={() => setOpen(false)} title="Explore Amaara" drawer>
        <nav className="s-mobile-nav" aria-label="Mobile navigation">
          {storefrontNavigation.map(([to, label]) => (
            <NavLink key={to} to={to} end={to === '/'} onClick={() => setOpen(false)}>
              {label}
              <Icon name="arrow" />
            </NavLink>
          ))}
          <Link className="s-mobile-search" to="/products" onClick={() => setOpen(false)}>
            <Icon name="search" />Search products
          </Link>
          {authStatus === 'loading' ? (
            <p>Restoring session…</p>
          ) : isAuthenticated ? (
            <div className="s-mobile-account">
              <p>Your account</p>
              <AccountLinks close={() => setOpen(false)} signOut={signOut} />
            </div>
          ) : (
            <div className="s-mobile-account">
              <Link to="/login" onClick={() => setOpen(false)}>Sign in</Link>
              <Link to="/register" onClick={() => setOpen(false)}>Create an account</Link>
            </div>
          )}
          <Link className="s-button s-button-primary" to="/custom" onClick={() => setOpen(false)}>
            Design custom stickers &amp; signs
          </Link>
        </nav>
      </Dialog>
    </>
  );
}
