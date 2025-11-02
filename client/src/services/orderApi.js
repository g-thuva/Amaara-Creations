import api from './api';

export const orderApi = {
  // Get user's orders
  getUserOrders: async () => {
    const response = await api.get('/orders');
    return response.data;
  },

  // Get order by ID
  getOrderById: async (id) => {
    const response = await api.get(`/orders/${id}`);
    return response.data;
  },

  // Create order (checkout)
  createOrder: async (orderData) => {
    const response = await api.post('/orders', orderData);
    return response.data;
  },

  // Update order status (Admin only)
  updateOrderStatus: async (id, status) => {
    const response = await api.put(`/orders/${id}/status`, { status });
    return response.data;
  },

  // Get all orders (Admin only)
  getAllOrders: async (params = {}) => {
    const { status, search, pageNumber = 1, pageSize = 20 } = params;
    const queryParams = new URLSearchParams();
    if (status) queryParams.append('status', status);
    if (search) queryParams.append('search', search);
    queryParams.append('pageNumber', pageNumber);
    queryParams.append('pageSize', pageSize);

    const response = await api.get(`/orders/admin/all?${queryParams.toString()}`);
    return response.data;
  },

  // Get order details (Admin only)
  getAdminOrderById: async (id) => {
    const response = await api.get(`/orders/admin/${id}`);
    return response.data;
  },
};

