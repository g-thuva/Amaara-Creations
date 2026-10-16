import api from "@/shared/api/httpClient.js";
export const reportsApi = {
  getReports: async (params, signal) =>
    (await api.get("/admin/reports", { params, signal })).data,
  exportOrders: async (params) =>
    (
      await api.get("/admin/reports/orders.csv", {
        params,
        responseType: "blob",
      })
    ).data,
};
