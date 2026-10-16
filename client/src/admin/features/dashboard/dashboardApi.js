import api from "../../../services/api.js";
export const dashboardApi = {
  getDashboardStats: async () => {
    const response = await api.get("/admin/dashboard");
    return response.data;
  },

  getRevenueStats: async () => {
    const response = await api.get("/admin/dashboard/revenue");
    return response.data;
  },

  getOrderStats: async () => {
    const response = await api.get("/admin/dashboard/orders");
    return response.data;
  },

  getProductStats: async () => {
    const response = await api.get("/admin/dashboard/products");
    return response.data;
  },

  getRecentOrders: async (limit = 10) => {
    const response = await api.get(
      `/admin/dashboard/recent-orders?limit=${limit}`,
    );
    return response.data;
  },
};
