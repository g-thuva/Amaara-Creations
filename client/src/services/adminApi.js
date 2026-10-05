import api from './api';

export const adminApi = {
  // Dashboard Statistics
  getDashboardStats: async () => {
    const response = await api.get('/admin/dashboard');
    return response.data;
  },

  // Revenue Statistics
  getRevenueStats: async () => {
    const response = await api.get('/admin/dashboard/revenue');
    return response.data;
  },

  // Order Statistics
  getOrderStats: async () => {
    const response = await api.get('/admin/dashboard/orders');
    return response.data;
  },

  // Product Statistics
  getProductStats: async () => {
    const response = await api.get('/admin/dashboard/products');
    return response.data;
  },

  // Recent Orders
  getRecentOrders: async (limit = 10) => {
    const response = await api.get(`/admin/dashboard/recent-orders?limit=${limit}`);
    return response.data;
  },

  // Customers Management
  getAllCustomers: async (params = {}) => {
    const { search, pageNumber = 1, pageSize = 20 } = params;
    const queryParams = new URLSearchParams();
    if (search) queryParams.append('search', search);
    queryParams.append('pageNumber', pageNumber);
    queryParams.append('pageSize', pageSize);

    const response = await api.get(`/admin/customers?${queryParams.toString()}`);
    return response.data;
  },

  getCustomerDetails: async (id) => {
    const response = await api.get(`/admin/customers/${id}`);
    return response.data;
  },

  getCustomerOrders: async (id, params = {}) => {
    const { pageNumber = 1, pageSize = 20 } = params;
    const response = await api.get(
      `/admin/customers/${id}/orders?pageNumber=${pageNumber}&pageSize=${pageSize}`
    );
    return response.data;
  },

  getCustomerStats: async (id) => {
    const response = await api.get(`/admin/customers/${id}/stats`);
    return response.data;
  },

  getProducts: async (params = {}) => {
    const response = await api.get('/admin/products', { params });
    return response.data;
  },

  getProduct: async (id) => {
    const response = await api.get(`/admin/products/${id}`);
    return response.data;
  },

  createProduct: async (payload) => {
    const response = await api.post('/admin/products', payload);
    return response.data;
  },

  updateProduct: async (id, payload) => {
    const response = await api.put(`/admin/products/${id}`, payload);
    return response.data;
  },

  archiveProduct: async (id) => {
    const response = await api.post(`/admin/products/${id}/archive`);
    return response.data;
  },

  reactivateProduct: async (id) => {
    const response = await api.post(`/admin/products/${id}/reactivate`);
    return response.data;
  },

  uploadProductMedia: async (id, file, fields = {}) => {
    const formData = new FormData();
    formData.append('file', file);
    Object.entries(fields).forEach(([key, value]) => formData.append(key, value));
    const response = await api.post(`/admin/products/${id}/media`, formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return response.data;
  },

  getProductMedia: async (id) => {
    const response = await api.get(`/admin/products/${id}/media`);
    return response.data;
  },

  updateProductMedia: async (productId, mediaId, payload) => {
    const response = await api.put(`/admin/products/${productId}/media/${mediaId}`, payload);
    return response.data;
  },

  getProductVariants: async (id) => {
    const response = await api.get(`/admin/products/${id}/variants`);
    return response.data;
  },

  createProductVariant: async (id, payload) => {
    const response = await api.post(`/admin/products/${id}/variants`, payload);
    return response.data;
  },

  updateProductVariant: async (productId, variantId, payload) => {
    const response = await api.put(`/admin/products/${productId}/variants/${variantId}`, payload);
    return response.data;
  },

  adjustProductStock: async (id, payload) => {
    const response = await api.post(`/admin/products/${id}/stock-adjustments`, payload);
    return response.data;
  },

  getCategories: async () => {
    const response = await api.get('/admin/categories');
    return response.data;
  },

  createCategory: async (payload) => {
    const response = await api.post('/admin/categories', payload);
    return response.data;
  },

  updateCategory: async (id, payload) => {
    const response = await api.put(`/admin/categories/${id}`, payload);
    return response.data;
  },

  archiveCategory: async (id) => {
    const response = await api.post(`/admin/categories/${id}/archive`);
    return response.data;
  },

  reactivateCategory: async (id) => {
    const response = await api.post(`/admin/categories/${id}/reactivate`);
    return response.data;
  },

  getCollections: async () => {
    const response = await api.get('/admin/collections');
    return response.data;
  },

  createCollection: async (payload) => {
    const response = await api.post('/admin/collections', payload);
    return response.data;
  },

  updateCollection: async (id, payload) => {
    const response = await api.put(`/admin/collections/${id}`, payload);
    return response.data;
  },

  replaceCollectionProducts: async (id, productIds) => {
    const response = await api.put(`/admin/collections/${id}/products`, { productIds });
    return response.data;
  },

  archiveCollection: async (id) => {
    const response = await api.post(`/admin/collections/${id}/archive`);
    return response.data;
  },

  reactivateCollection: async (id) => {
    const response = await api.post(`/admin/collections/${id}/reactivate`);
    return response.data;
  },

  getMedia: async (params = {}) => {
    const response = await api.get('/admin/media', { params });
    return response.data;
  },

  getPages: async () => {
    const response = await api.get('/admin/content/pages');
    return response.data;
  },

  createPage: async (payload) => {
    const response = await api.post('/admin/content/pages', payload);
    return response.data;
  },

  updatePage: async (id, payload) => {
    const response = await api.put(`/admin/content/pages/${id}`, payload);
    return response.data;
  },

  getSettings: async () => {
    const response = await api.get('/admin/settings');
    return response.data;
  },

  saveSetting: async (payload) => {
    const response = await api.put(`/admin/settings/${encodeURIComponent(payload.key)}`, payload);
    return response.data;
  },
};

