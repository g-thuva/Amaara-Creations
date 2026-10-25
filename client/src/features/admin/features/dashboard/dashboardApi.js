import api from "@/shared/api/httpClient.js";
export const dashboardApi = {
  getDashboardStats: async (signal) => {
    const response = await api.get("/admin/dashboard", { signal });
    return response.data;
  },
  getRevenueStats: async (signal) =>
    (await api.get("/admin/dashboard/revenue", { signal })).data,
  getOrderStats: async (signal) =>
    (await api.get("/admin/dashboard/orders", { signal })).data,
  getProductStats: async (signal) => {
    const response = await api.get("/admin/dashboard/products", { signal });
    return response.data;
  },

  getRecentOrders: async (limit = 10, signal) => {
    const response = await api.get(
      `/admin/dashboard/recent-orders?limit=${limit}`,
      { signal },
    );
    return response.data;
  },
};
