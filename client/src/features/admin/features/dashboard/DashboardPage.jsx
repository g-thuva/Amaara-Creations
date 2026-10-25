import { useMemo, useState } from 'react';
import { Link } from 'react-router-dom';
import { useAuth } from '@/features/auth/AuthProvider.jsx';
import { can } from '@/app/config/permissions.js';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { ErrorState } from '@/features/admin/shared/components/ErrorState.jsx';
import { BarChart } from '@/features/admin/shared/components/charts/BarChart.jsx';
import { DonutChart } from '@/features/admin/shared/components/charts/DonutChart.jsx';
import { TrendChip } from '@/features/admin/shared/components/charts/TrendChip.jsx';
import { dashboardPeriod, periodLabel, COLOMBO } from '@/utils/dateBuckets.js';
import { formatMoney } from '@/utils/formatMoney.js';
import { useDashboardData } from './useDashboardData.js';
import { salesFromLegacy, productSnapshot } from './dashboardAdapter.js';
import { DashboardSummary } from './DashboardSummary.jsx';
import { RecentOrdersTable } from './RecentOrdersTable.jsx';
import { TopProductsList } from './TopProductsList.jsx';
import './Dashboard.css';

function SalesBreakdown({ sales, resource, loading, error, retry }) {
  const products = productSnapshot(resource.data);
  const snapshot = products.slice(0, 5).map(product => ({ label: product.productName, value: product.totalRevenue }));
  if (products.length > 5) snapshot.push({ label: 'Others (in top 10)', value: products.slice(5).reduce((sum, product) => sum + product.totalRevenue, 0) });
  return <section className="sales-panel sales-breakdown"><div className="sales-panel-header"><div><h2>Sales breakdown</h2><p>Revenue share · selected period</p></div>{sales.status && <span className="sales-scope-badge">By status</span>}</div>
    {loading ? <div className="sales-products-skeleton" role="status" aria-label="Loading sales breakdown"><span className="admin-skeleton" /></div> : error ? <ErrorState title="Sales breakdown could not be loaded" retry={retry} /> : <DonutChart items={sales.status || []} emptyText={sales.reason ? 'Selected-period sales shares are not available' : 'No sales to break down yet'} />}
    {!loading && !error && snapshot.length > 0 && <details className="sales-breakdown-note"><summary>Available: all-time product share</summary><p>Share of the returned top 10 products’ revenue. This snapshot does not follow the selected period.</p><DonutChart items={snapshot} label="Top 10 revenue" /></details>}
    {!loading && !error && sales.reason && <p className="sales-data-note">Status counts cannot be used as revenue shares.</p>}
  </section>;
}

export default function DashboardPage() {
  const { user } = useAuth();
  const resources = useDashboardData();
  const [period, setPeriod] = useState('30D');
  const [now] = useState(() => new Date());
  const range = useMemo(() => dashboardPeriod(period, now), [period, now]);
  const sales = salesFromLegacy(range, resources.orders.data);
  const canViewOrders = can(user, 'ViewOrders');
  const canManageCatalog = can(user, 'ManageCatalog');
  const chartLoading = resources.revenue.loading || resources.orders.loading;
  const chartError = resources.revenue.error || resources.orders.error;
  const retryChart = () => { if (resources.revenue.error) resources.revenue.reload(); if (resources.orders.error) resources.orders.reload(); };
  const today = new Intl.DateTimeFormat('en-GB', { timeZone: COLOMBO, weekday: 'long', day: 'numeric', month: 'long', year: 'numeric' }).format(now);
  return <div className="sales-command">
    <header className="sales-command-header"><div><h1>Dashboard</h1><p>Welcome back, {user?.name?.split(' ')[0] || 'there'} · {today}</p></div><div className="sales-header-actions">
      <div className="sales-attention" role="group" aria-label="Needs attention">{resources.orders.data?.pendingOrders > 0 && canViewOrders && <Link to="/admin/orders?status=Pending"><AppIcon name="pause" />{resources.orders.data.pendingOrders} pending</Link>}{resources.products.data?.lowStockProducts > 0 && can(user, 'ManageInventory') && <Link to="/admin/inventory"><AppIcon name="package" />{resources.products.data.lowStockProducts} low stock</Link>}</div>
      <div className="sales-period-control" role="group" aria-label="Dashboard period">{['7D', '30D', '3M', '12M'].map(option => <button type="button" key={option} aria-pressed={period === option} onClick={() => setPeriod(option)}>{option}</button>)}</div>
    </div></header>
    <DashboardSummary {...resources} sales={sales} />
    <div className="sales-chart-row"><section className="sales-panel sales-overview" aria-label="Sales overview"><div className="sales-panel-header"><div><h2>Sales overview</h2><p>{periodLabel(range)} · Asia/Colombo</p></div><span className="sales-metric-label">Order value</span></div>
      <div className="sales-headline-row"><strong className="sales-headline">{chartLoading ? '—' : formatMoney(sales.total)}</strong><TrendChip current={sales.total} previous={sales.previousTotal} description /></div>
      <p className="sales-money-note">Order totals include unpaid orders; settled funds are shown separately</p>
      <div className="sales-chart-transition" key={period}><BarChart data={sales.series} previous={sales.previous} monthly={range.monthly} days={range.days} loading={chartLoading} error={chartError} retry={retryChart} unavailable={sales.reason} canViewOrders={canViewOrders} /></div>
    </section><SalesBreakdown sales={sales} resource={resources.products} loading={resources.orders.loading} error={resources.orders.error} retry={resources.orders.reload} /></div>
    <div className="sales-bottom-row"><RecentOrdersTable resource={resources.recent} canViewOrders={canViewOrders} /><TopProductsList resource={resources.products} canManageCatalog={canManageCatalog} /></div>
  </div>;
}
