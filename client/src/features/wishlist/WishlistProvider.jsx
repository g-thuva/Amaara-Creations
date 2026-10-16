/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext } from 'react';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { useResource } from '@/shared/hooks/useResource.js';
import { wishlistApi } from '@/features/wishlist/wishlistApi.js';

const WishlistContext = createContext(null);
export function WishlistProvider({ enabled, children }) {
  const { user, isAuthenticated } = useAuth();
  const wishlist = useResource(wishlistApi.getWishlist, user?.id || '', isAuthenticated && enabled);
  const { mutate, pending } = useAuthenticatedMutation();
  const toggleWishlist = id => {
    const saved = wishlist.data?.items.some(item => item.productId === id);
    return mutate(() => saved ? wishlistApi.removeFromWishlist(id) : wishlistApi.addToWishlist(id),
      wishlist.reload, saved ? 'Removed from your wishlist.' : 'Saved to your wishlist.');
  };
  return <WishlistContext.Provider value={{ wishlist, toggleWishlist, pending }}>{children}</WishlistContext.Provider>;
}
export const useWishlist = () => useContext(WishlistContext);
