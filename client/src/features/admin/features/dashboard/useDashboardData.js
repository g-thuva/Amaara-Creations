import { useResource } from '@/shared/hooks/useResource.js';
import { dashboardApi } from './dashboardApi.js';
/** Independent effects start in parallel; retry never discards other cards. */
export function useDashboardData() {
  const summary = useResource(signal => dashboardApi.getDashboardStats(signal), 'dashboard-summary');
  const revenue = useResource(signal => dashboardApi.getRevenueStats(signal), 'dashboard-revenue');
  const orders = useResource(signal => dashboardApi.getOrderStats(signal), 'dashboard-orders');
  const products = useResource(signal => dashboardApi.getProductStats(signal), 'dashboard-products');
  const recent = useResource(signal => dashboardApi.getRecentOrders(5, signal), 'dashboard-recent');
  return { summary, revenue, orders, products, recent };
}
