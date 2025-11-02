import api from './api';

export const reviewApi = {
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

  // Get all reviews (Admin only)
  getAllReviews: async (params = {}) => {
    const { productId, rating, search, pageNumber = 1, pageSize = 20 } = params;
    const queryParams = new URLSearchParams();
    if (productId) queryParams.append('productId', productId);
    if (rating) queryParams.append('rating', rating);
    if (search) queryParams.append('search', search);
    queryParams.append('pageNumber', pageNumber);
    queryParams.append('pageSize', pageSize);

    const response = await api.get(`/admin/reviews?${queryParams.toString()}`);
    return response.data;
  },

  // Get review statistics (Admin only)
  getReviewStats: async () => {
    const response = await api.get('/admin/reviews/stats');
    return response.data;
  },
};

