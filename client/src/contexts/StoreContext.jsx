/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext, useRef, useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from './AuthContext';
import { useResource } from '../hooks/useResource';
import { cartApi } from '../services/cartApi';
import { wishlistApi } from '../services/wishlistApi';
import { contentApi } from '../services/contentApi';
import { customerError } from '../utils/storefront';

const StoreContext = createContext(null);
export function StoreProvider({ children }) {
  const { user, isAuthenticated } = useAuth();
  const location = useLocation();
  const navigate = useNavigate();
  const isStore = !location.pathname.startsWith('/admin');
  const settings = useResource(contentApi.settings, 'settings', isStore);
  const cart = useResource(cartApi.getCart, user?.id || '', isAuthenticated && isStore);
  const wishlist = useResource(wishlistApi.getWishlist, user?.id || '', isAuthenticated && isStore);
  const [notice, setNotice] = useState(null);
  const [pending, setPending] = useState(false);
  const locked = useRef(false);
  const notify = (message, type = 'success') => setNotice({ message, type });
  const requireLogin = () => {
    if (isAuthenticated) return true;
    navigate('/login', { state: { from: location.pathname + location.search } });
    return false;
  };
  const mutate = async (action, refresh, message) => {
    if (!requireLogin() || locked.current) return false;
    locked.current = true;
    setPending(true);
    try { await action(); refresh?.(); if (message) notify(message); return true; }
    catch (error) { notify(customerError(error), 'error'); return false; }
    finally { locked.current = false; setPending(false); }
  };
  const toggleWishlist = async (id) => {
    const saved = wishlist.data?.items.some(item => item.productId === id);
    return mutate(() => saved ? wishlistApi.removeFromWishlist(id) : wishlistApi.addToWishlist(id), wishlist.reload, saved ? 'Removed from your wishlist.' : 'Saved to your wishlist.');
  };
  const values = Object.fromEntries((settings.data || []).map(item => [item.key, item.value]));
  return <StoreContext.Provider value={{ settings, values, cart, wishlist, pending, mutate, notify, toggleWishlist, requireLogin }}>
    {children}
    {notice && <div className={`store-toast ${notice.type}`} role={notice.type === 'error' ? 'alert' : 'status'}>
      <span>{notice.message}</span><button type="button" aria-label="Dismiss notification" onClick={() => setNotice(null)}>×</button>
    </div>}
  </StoreContext.Provider>;
}
export const useStore = () => useContext(StoreContext);
