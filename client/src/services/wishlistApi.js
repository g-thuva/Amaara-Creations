import api from './api';

export const wishlistApi = {
  // Get user's wishlist
  getWishlist: async () => {
    const response = await api.get('/wishlist');
    return response.data;
  },

  // Add product to wishlist
  addToWishlist: async (productId) => {
    const response = await api.post('/wishlist', { productId });
    return response.data;
  },

  // Remove product from wishlist
  removeFromWishlist: async (productId) => {
    const response = await api.delete(`/wishlist/${productId}`);
    return response.data;
  },

  // Add wishlist item to cart
  addWishlistItemToCart: async (productId, quantity = 1) => {
    const response = await api.post(`/wishlist/${productId}/cart?quantity=${quantity}`);
    return response.data;
  },

  // Clear entire wishlist
  clearWishlist: async () => {
    const response = await api.delete('/wishlist');
    return response.data;
  },
};

