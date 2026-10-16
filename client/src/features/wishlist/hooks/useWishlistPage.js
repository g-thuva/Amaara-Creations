import { useCart } from '@/features/cart/CartProvider.jsx';
import { useWishlist } from '@/features/wishlist/WishlistProvider.jsx';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { wishlistApi } from '@/features/wishlist/wishlistApi.js';

export function useWishlistPage() {
  const { cart } = useCart();
  const { wishlist } = useWishlist();
  const { mutate, pending } = useAuthenticatedMutation();
  const moveToCart = productId => mutate(() => wishlistApi.addWishlistItemToCart(productId), () => { cart.reload(); wishlist.reload(); }, 'Added to your cart.');
  const remove = productId => mutate(() => wishlistApi.removeFromWishlist(productId), wishlist.reload, 'Removed from your wishlist.');
  return { wishlist, pending, moveToCart, remove };
}
