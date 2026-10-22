import { StatCard } from '@/features/admin/shared/components/AdminUi.jsx';
import { formatIsoCurrency as money } from '@/features/admin/shared/utils/format.js';
export function DashboardSummary({ dashboardStats }) {
  const stats = dashboardStats || {};
  return <div className="stats-grid"><StatCard icon="chart" label="LKR order value" value={money(stats.totalRevenue || 0, 'LKR')} description={'This month: ' + money(stats.revenueThisMonth || 0, 'LKR')} /><StatCard icon="bag" label="Total Orders" value={(stats.totalOrders || 0).toLocaleString('en-GB')} description={'This month: ' + (stats.ordersThisMonth || 0)} /><StatCard icon="users" label="Total Customers" value={(stats.totalCustomers || 0).toLocaleString('en-GB')} description="Registered accounts" /><StatCard icon="star" label="Avg. Rating" value={Number(stats.averageRating || 0).toFixed(1)} description="Published customer reviews" /></div>;
}
