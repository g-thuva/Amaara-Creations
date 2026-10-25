import { useEffect, useRef, useState } from 'react';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { ErrorState } from '@/features/admin/shared/components/ErrorState.jsx';
import { TrendChip } from '@/features/admin/shared/components/charts/TrendChip.jsx';
import { Sparkline } from '@/features/admin/shared/components/charts/Sparkline.jsx';
import { formatMoney } from '@/utils/formatMoney.js';

function CountUp({ value, format }) {
  const [frame, setFrame] = useState(null);
  const animated = useRef(false);
  useEffect(() => {
    if (!Number.isFinite(value) || animated.current) return;
    animated.current = true;
    if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) return;
    let request;
    const start = performance.now();
    const tick = now => {
      const progress = Math.min(1, (now - start) / 600);
      setFrame(progress === 1 ? null : value * (1 - (1 - progress) ** 3));
      if (progress < 1) request = requestAnimationFrame(tick);
    };
    request = requestAnimationFrame(tick);
    return () => cancelAnimationFrame(request);
  }, [value]);
  return <><span aria-hidden="true">{format(frame ?? value)}</span><span className="admin-sr-only">{format(value)}</span></>;
}
function StatCard({ label, icon, value, format, previous, scope, resource, series }) {
  return <article className="sales-stat" aria-label={label} aria-busy={resource.loading || undefined}>
    <div className="sales-stat-heading"><span className="sales-stat-icon"><AppIcon name={icon} /></span><span>{label}</span></div>
    {resource.loading ? <><span className="admin-skeleton" /><span className="admin-sr-only">Loading {label}</span></> : resource.error ? <ErrorState title={`${label} unavailable`} retry={resource.reload} /> : <>
      <strong className="sales-stat-value"><CountUp value={value} format={format} /></strong>
      <TrendChip current={value} previous={previous} description />
      <span className="sales-stat-scope">{scope}</span>
      {series && <Sparkline values={series} />}
    </>}
  </article>;
}
const count = value => Number.isFinite(value) ? Math.round(value).toLocaleString('en-GB') : '—';
const rating = value => Number.isFinite(value) ? `${value.toFixed(1)} / 5` : '—';
export function DashboardSummary({ summary, revenue, orders, sales }) {
  const stats = summary.data;
  const scoped = sales.total !== null;
  return <div className="sales-kpis">
    <StatCard icon="chart" label="Order value" value={scoped ? sales.total : revenue.data?.totalRevenue} previous={sales.previousTotal} format={formatMoney} resource={scoped ? orders : revenue} scope={scoped ? 'Selected period · CHF business orders' : 'All time · period comparison unavailable'} />
    <StatCard icon="bag" label="Total orders" value={sales.orderCount ?? orders.data?.totalOrders} previous={scoped ? sales.previous?.reduce((sum, bucket) => sum + bucket.orders, 0) : null} format={count} resource={orders} scope={scoped ? 'Selected period' : 'All time · all currencies and environments'} />
    <StatCard icon="users" label="Total customers" value={stats?.totalCustomers} format={count} resource={summary} scope="All time · registered accounts" />
    <StatCard icon="star" label="Avg. rating" value={stats?.averageRating} format={rating} resource={summary} scope="All time · customer reviews" />
  </div>;
}
