import { useDashboard } from "./useDashboard.js";
import { DashboardSummary } from "./DashboardSummary.jsx";
import { RecentOrdersTable } from "./RecentOrdersTable.jsx";
import { TopProductsList } from "./TopProductsList.jsx";
import { ResourceState } from "../../shared/components/ResourceState.jsx";
import styles from "./Dashboard.module.css";
export default function DashboardPage() {
  const resource = useDashboard();
  return (
    <div className={`dashboard ${styles.dashboard}`}>
      <div className="dashboard-header">
        <h1>Dashboard</h1>
        <p>Welcome back! Here's what's happening with your store today.</p>
      </div>
      <ResourceState resource={resource}>
        {resource.data && (
          <>
            <DashboardSummary dashboardStats={resource.data.stats} />
            <div className="dashboard-row">
              <RecentOrdersTable recentOrders={resource.data.recentOrders} />
              <TopProductsList topProducts={resource.data.topProducts} />
            </div>
          </>
        )}
      </ResourceState>
    </div>
  );
}
