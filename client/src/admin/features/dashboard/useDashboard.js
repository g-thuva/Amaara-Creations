import { useResource } from "../../../hooks/useResource.js";
import { dashboardApi } from "./dashboardApi.js";
import { reportClientError } from "../../../services/clientTelemetry.js";
export function useDashboard() {
  return useResource(async () => {
    try {
      const [stats, recentOrders, productStats] = await Promise.all([
        dashboardApi.getDashboardStats(),
        dashboardApi.getRecentOrders(5),
        dashboardApi.getProductStats(),
      ]);
      return {
        stats,
        recentOrders: recentOrders || [],
        topProducts: productStats.topSellingProducts || [],
      };
    } catch (error) {
      reportClientError("admin.dashboard", error);
      throw error;
    }
  }, "dashboard");
}
