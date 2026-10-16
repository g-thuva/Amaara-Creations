import { useLocation } from 'react-router-dom';
import { CartProvider } from '@/features/cart/CartProvider.jsx';
import { WishlistProvider } from '@/features/wishlist/WishlistProvider.jsx';
import { SiteContentProvider } from '@/features/content/SiteContentProvider.jsx';
import { ToastProvider } from '@/shared/components/feedback/ToastProvider.jsx';

export function StorefrontProviders({ children }) {
  const { pathname } = useLocation();
  const enabled = !pathname.startsWith('/admin');
  return <ToastProvider>
    <SiteContentProvider enabled={enabled}>
      <CartProvider enabled={enabled}>
        <WishlistProvider enabled={enabled}>{children}</WishlistProvider>
      </CartProvider>
    </SiteContentProvider>
  </ToastProvider>;
}
