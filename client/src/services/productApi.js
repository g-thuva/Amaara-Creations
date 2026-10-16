import api from './api';

export const productApi = {
  // Get all products with pagination, search, and filter
  getProducts: async (params = {}, signal) => {
    const response = await api.get('/products', { params, signal });
    return response.data;
  },

  // Get product by ID
  getProductById: async (id, signal) => {
    const response = await api.get(`/products/${encodeURIComponent(id)}`, { signal });
    return response.data;
  },

};
