import AppIcon from "@/shared/components/ui/Icon.jsx";
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
export function DashboardSummary({ dashboardStats }) {
  const stats = [
    {
      title: "LKR order value",
      value: checkoutMoney(dashboardStats?.totalRevenue || 0, "LKR"),
      change:
        "This month: " +
        checkoutMoney(dashboardStats?.revenueThisMonth || 0, "LKR"),
      trend: "neutral",
      icon: <AppIcon name="chart" />,
    },
    {
      title: "Total Orders",
      value: dashboardStats?.totalOrders?.toLocaleString() || "0",
      change: "This month: " + (dashboardStats?.ordersThisMonth || 0),
      trend: "neutral",
      icon: <AppIcon name="bag" />,
    },
    {
      title: "Total Customers",
      value: dashboardStats?.totalCustomers?.toLocaleString() || "0",
      change: "Registered accounts",
      trend: "neutral",
      icon: <AppIcon name="users" />,
    },
    {
      title: "Avg. Rating",
      value: dashboardStats?.averageRating?.toFixed(1) || "0.0",
      change: "Published customer reviews",
      trend: "neutral",
      icon: <AppIcon name="star" />,
    },
  ];

  return (
    <div className="stats-grid">
      {stats.map((stat, index) => (
        <div key={index} className="stat-card">
          <div
            className="stat-icon"
            style={{ background: "rgba(139, 90, 43, 0.1)", color: "#8b5a2b" }}
          >
            {stat.icon}
          </div>
          <div className="stat-info">
            <span className="stat-value">{stat.value}</span>
            <span className="stat-title">{stat.title}</span>
          </div>
          <div className={`stat-change ${stat.trend}`}>
            <AppIcon name="chart" />
            <span>{stat.change}</span>
          </div>
        </div>
      ))}
    </div>
  );
}
