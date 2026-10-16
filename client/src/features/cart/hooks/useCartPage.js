import { catalogApi } from '@/features/catalog/catalogApi.js';
import { useState } from 'react';
import { useCart } from '@/features/cart/CartProvider.jsx';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { useResource } from '@/shared/hooks/useResource.js';
import { cartApi } from '@/features/cart/cartApi.js';

export function useCartPage() {
  const { cart } = useCart();
  const { mutate, pending } = useAuthenticatedMutation();
  const categories = useResource(catalogApi.getCategories, 'cart-categories');
  const [confirm, setConfirm] = useState(null);
  const invalid = cart.data?.items.some((item) => item.isOutOfStock || item.quantity > item.productStock);
  const currencyMismatch = cart.data?.canCheckout === false && !!cart.data?.items.length;

  const confirmRemoval = async () => {
    const action = () => confirm === 'all' ? cartApi.clearCart() : cartApi.removeFromCart(confirm.id);
    if (await mutate(action, cart.reload, 'Your cart has been updated.')) setConfirm(null);
  };

  const updateQuantity = (id, quantity) => mutate(() => cartApi.updateCartItem(id, quantity), cart.reload);
  return { cart, pending, categories, confirm, setConfirm, invalid, currencyMismatch, confirmRemoval, updateQuantity };
}
