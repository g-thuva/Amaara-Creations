/* eslint-disable react-refresh/only-export-components */
import { createContext, useContext } from 'react';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { useResource } from '@/shared/hooks/useResource.js';
import { cartApi } from '@/features/cart/cartApi.js';

const CartContext = createContext(null);
export function CartProvider({ enabled, children }) {
  const { user, isAuthenticated } = useAuth();
  const cart = useResource(cartApi.getCart, user?.id || '', isAuthenticated && enabled);
  return <CartContext.Provider value={{ cart }}>{children}</CartContext.Provider>;
}
export const useCart = () => useContext(CartContext);
