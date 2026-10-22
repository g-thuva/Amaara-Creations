import { useDashboard } from "@/features/admin/features/dashboard/useDashboard.js";
import { DashboardSummary } from "@/features/admin/features/dashboard/DashboardSummary.jsx";
import { RecentOrdersTable } from "@/features/admin/features/dashboard/RecentOrdersTable.jsx";
import { TopProductsList } from "@/features/admin/features/dashboard/TopProductsList.jsx";
import { ResourceState } from "@/features/admin/shared/components/ResourceState.jsx";
import styles from "@/features/admin/features/dashboard/Dashboard.module.css";
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { PageHeader } from '@/features/admin/shared/components/PageHeader.jsx';
import { formatDate } from '@/features/admin/shared/utils/format.js';
import { can } from '@/app/config/permissions.js';
import { Link } from 'react-router-dom';
import { StatusChip } from '@/features/admin/shared/components/StatusChip.jsx';
export default function DashboardPage() {
  const resource = useDashboard();
  const { user } = useAuth();
  return (
    <div className={`dashboard ${styles.dashboard}`}>
      <PageHeader title="Dashboard" eyebrow={formatDate(new Date())} description={`Welcome back, ${user?.name?.split(' ')[0] || 'there'}. Here’s your store at a glance.`} />
      <ResourceState resource={resource}>
        {resource.data && (
          <>
            <DashboardSummary dashboardStats={resource.data.stats} />
            {resource.data.stats?.pendingOrders > 0 && <section className="card card-body"><div className="header-content"><div><h2>Needs attention</h2><p><StatusChip status="Pending" /> {resource.data.stats.pendingOrders} pending orders</p></div>{can(user, 'ViewOrders') && <Link className="btn btn-outline-primary" to="/admin/orders">Review orders</Link>}</div></section>}
            <div className="dashboard-row">
              <RecentOrdersTable recentOrders={resource.data.recentOrders} canViewOrders={can(user, 'ViewOrders')} />
              <TopProductsList topProducts={resource.data.topProducts} canManageCatalog={can(user, 'ManageCatalog')} />
            </div>
          </>
        )}
      </ResourceState>
    </div>
  );
}
