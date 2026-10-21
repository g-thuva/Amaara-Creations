import api from '@/shared/api/httpClient.js';

export const cartApi = {
  downloadArtwork: async (url, signal) => {
    if (!/^\/api\/v1\/custom-designs\/\d+\/assets\/\d+$/.test(url)) throw new Error('Invalid private artwork reference.');
    return (await api.get(url.replace(/^\/api\/v1/, ''), { responseType: 'blob', signal })).data;
  },
  // Get user's cart
  getCart: async () => {
    const response = await api.get('/cart');
    return response.data;
  },

  // Add item to cart
  addToCart: async (productId, quantity = 1, productVariantId = null) => {
    const response = await api.post('/cart', {
      productId,
      productVariantId,
      quantity,
    });
    return response.data;
  },

  // Update cart item quantity
  updateCartItem: async (itemId, quantity) => {
    const response = await api.put(`/cart/${itemId}`, { quantity });
    return response.data;
  },

  // Remove item from cart
  removeFromCart: async (itemId) => {
    const response = await api.delete(`/cart/${itemId}`);
    return response.data;
  },

  // Clear entire cart
  clearCart: async () => {
    const response = await api.delete('/cart');
    return response.data;
  },
};

