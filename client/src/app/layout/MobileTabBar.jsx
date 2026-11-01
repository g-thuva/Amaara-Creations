import { NavLink } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';
import { useCart } from '@/features/cart/CartProvider.jsx';
import { useWishlist } from '@/features/wishlist/WishlistProvider.jsx';

export default function MobileTabBar() {
  const { cart } = useCart();
  const { wishlist } = useWishlist();

  const cartCount = cart.data?.totalItems || 0;
  const wishlistCount = wishlist.data?.totalItems || 0;

  return (
    <nav className="s-mobile-tab-bar" aria-label="Mobile navigation bar">
      <NavLink to="/" end className={({ isActive }) => `s-tab-item ${isActive ? 'is-active' : ''}`}>
        <span className="s-tab-icon"><Icon name="home" strokeWidth={2.4} /></span>
        <span className="s-tab-label">Home</span>
      </NavLink>
      <NavLink to="/products" className={({ isActive }) => `s-tab-item ${isActive ? 'is-active' : ''}`}>
        <span className="s-tab-icon"><Icon name="search" strokeWidth={2.4} /></span>
        <span className="s-tab-label">Shop</span>
      </NavLink>
      <NavLink to="/custom" className={({ isActive }) => `s-tab-item s-tab-item-studio ${isActive ? 'is-active' : ''}`}>
        <span className="s-tab-icon s-tab-studio-icon"><Icon name="edit" strokeWidth={2.4} /></span>
        <span className="s-tab-label">Studio</span>
      </NavLink>
      <NavLink to="/wishlist" className={({ isActive }) => `s-tab-item ${isActive ? 'is-active' : ''}`}>
        <span className="s-tab-icon">
          <Icon name="heart" strokeWidth={2.4} />
          {wishlistCount > 0 && <span className="s-tab-badge">{wishlistCount}</span>}
        </span>
        <span className="s-tab-label">Saved</span>
      </NavLink>
      <NavLink to="/cart" className={({ isActive }) => `s-tab-item ${isActive ? 'is-active' : ''}`}>
        <span className="s-tab-icon">
          <Icon name="bag" strokeWidth={2.4} />
          {cartCount > 0 && <span className="s-tab-badge">{cartCount}</span>}
        </span>
        <span className="s-tab-label">Cart</span>
      </NavLink>
    </nav>
  );
}

