import api from '@/shared/api/httpClient.js';

export const reviewsApi = {
  getOwnReviews: async (signal, productId) => (await api.get('/reviews/mine', { signal, params: { productId } })).data,
  // Get product reviews
  getProductReviews: async (productId) => {
    const response = await api.get(`/products/${productId}/reviews`);
    return response.data;
  },

  // Create review
  createReview: async (productId, reviewData) => {
    const response = await api.post(`/products/${productId}/reviews`, reviewData);
    return response.data;
  },

  // Update review
  updateReview: async (reviewId, reviewData) => {
    const response = await api.put(`/reviews/${reviewId}`, reviewData);
    return response.data;
  },

  // Delete review
  deleteReview: async (reviewId) => {
    const response = await api.delete(`/reviews/${reviewId}`);
    return response.data;
  },

};
