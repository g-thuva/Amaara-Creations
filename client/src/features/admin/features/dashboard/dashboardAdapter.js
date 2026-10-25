// @ts-check
import { bucketOrders, monthlyBuckets, utcInstant } from '@/utils/dateBuckets.js';

export const SERIES_GAP = 'Daily sales are not available. The current report supplies UTC monthly totals; recent orders are a sample without payment environments.';
/**
 * Accepts only verified complete LKR data with an explicit environment and
 * coverage of BOTH periods. The current legacy endpoints cannot provide it.
 * @param {ReturnType<import('@/utils/dateBuckets.js').dashboardPeriod>} range
 * @param {import('./dashboardContracts.ts').ScopedSales | null} source
 * @returns {import('./dashboardContracts.ts').SalesView}
 */
export function adaptSales(range, source = null) {
  const unavailable = { series: null, previous: null, total: null, previousTotal: null, orderCount: null, status: null, reason: SERIES_GAP };
  if (!source?.complete || source.currency !== 'LKR' || !source.environment) return unavailable;
  const from = utcInstant(source.from), to = utcInstant(source.to);
  if (!from || !to || +from > Date.parse(range.previousStart) || +to < Date.parse(range.end)) return unavailable;
  if (source.orders.some(order => order.currency !== 'LKR' || order.environment !== source.environment || !Number.isFinite(order.total) || order.total < 0 || !utcInstant(order.orderDate))) return unavailable;
  const previousRange = { ...range, start: range.previousStart, end: range.start };
  const eligible = source.orders.filter(order => order.status !== 'Cancelled');
  const days = bucketOrders(eligible, range);
  const previousDays = bucketOrders(eligible, previousRange);
  const selected = eligible.filter(order => { const date = utcInstant(order.orderDate); return date && +date >= Date.parse(range.start) && +date < Date.parse(range.end); });
  const status = new Map();
  selected.forEach(order => status.set(order.status, (status.get(order.status) || 0) + order.total));
  return {
    series: range.monthly ? monthlyBuckets(days) : days,
    // Unequal month lengths must not be compared bucket-by-bucket. Total
    // comparisons still use exactly equal calendar-day spans.
    previous: range.monthly ? null : previousDays,
    total: days.reduce((sum, day) => sum + day.value, 0),
    previousTotal: previousDays.reduce((sum, day) => sum + day.value, 0),
    orderCount: selected.length,
    status: [...status].map(([label, value]) => ({ label, value })),
    reason: null,
  };
}

/** All-time zero order counts prove an empty complete list without sampling.
 * @param {ReturnType<import('@/utils/dateBuckets.js').dashboardPeriod>} range
 * @param {{totalOrders: number} | null} orderStats
 */
export function salesFromLegacy(range, orderStats) {
  return adaptSales(range, orderStats?.totalOrders === 0 ? {
    currency: 'LKR', environment: 'business', complete: true,
    from: range.previousStart, to: range.end, orders: [],
  } : null);
}

/** Never infer monetary status shares from the legacy status COUNTS.
 * @param {{topSellingProducts: import('./dashboardContracts.ts').TopProduct[]} | null} products
 */
export function productSnapshot(products) {
  return (products?.topSellingProducts || []).filter(product => Number.isFinite(product.totalRevenue) && product.totalRevenue >= 0)
    .slice().sort((a, b) => b.totalRevenue - a.totalRevenue);
}
