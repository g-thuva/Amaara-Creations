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
};

