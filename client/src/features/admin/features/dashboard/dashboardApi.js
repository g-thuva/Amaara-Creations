import api from "@/shared/api/httpClient.js";
export const dashboardApi = {
  getDashboardStats: async () => {
    const response = await api.get("/admin/dashboard");
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
