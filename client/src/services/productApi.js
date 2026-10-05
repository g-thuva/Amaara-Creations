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

  // Create product (Admin only)
  createProduct: async (productData) => {
    const response = await api.post('/admin/products', productData);
    return response.data;
  },

  // Update product (Admin only)
  updateProduct: async (id, productData) => {
    const response = await api.put(`/admin/products/${id}`, productData);
    return response.data;
  },

  // Archive product (Admin only)
  deleteProduct: async (id) => {
    const response = await api.post(`/admin/products/${id}/archive`);
    return response.data;
  },
};

