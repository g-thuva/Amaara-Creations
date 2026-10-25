import { describe, expect, it } from 'vitest';
import { dashboardPeriod } from '@/utils/dateBuckets.js';
import { adaptSales, salesFromLegacy } from './dashboardAdapter.js';
const range = dashboardPeriod('7D', new Date('2026-10-23T05:00:00Z'));
const source = { complete: true, currency: 'CHF', environment: 'LIVE', from: range.previousStart, to: range.end, orders: [
  { orderDate: '2026-10-22T18:30:00', total: 20, status: 'Pending', currency: 'CHF', environment: 'LIVE' },
  { orderDate: '2026-10-23T05:00:00Z', total: 100, status: 'Cancelled', currency: 'CHF', environment: 'LIVE' },
  { orderDate: '2026-10-13T05:00:00Z', total: 10, status: 'Delivered', currency: 'CHF', environment: 'LIVE' },
] };
describe('honest dashboard adaptation', () => {
  it('does not turn a recent sample or legacy UTC monthly totals into local daily sales', () => {
    expect(adaptSales(range).series).toBeNull(); expect(salesFromLegacy(range, { totalOrders: 3 }).total).toBeNull();
  });
  it('can establish a real empty state when there are no orders at all', () => {
    const view = salesFromLegacy(range, { totalOrders: 0 }); expect(view.series).toHaveLength(7); expect(view.total).toBe(0); expect(view.previousTotal).toBe(0); expect(view.orderCount).toBe(0);
  });
  it('counts unpaid business orders and excludes cancellations with equal coverage', () => {
    const view = adaptSales(range, source); expect(view.total).toBe(20); expect(view.previousTotal).toBe(10); expect(view.orderCount).toBe(1); expect(view.status).toEqual([{ label: 'Pending', value: 20 }]); expect(view.series.at(-1).value).toBe(20);
  });
  it.each([{ complete: false }, { currency: 'USD' }, { currency: 'LKR' }, { environment: '' }, { from: range.start }, { to: range.start }, { from: 'invalid' }])('rejects incomplete or unscoped input %j', changes => expect(adaptSales(range, { ...source, ...changes }).series).toBeNull());
  it('never mixes currency or payment environment', () => {
    expect(adaptSales(range, { ...source, orders: [...source.orders, { ...source.orders[0], currency: 'USD' }] }).series).toBeNull();
    expect(adaptSales(range, { ...source, orders: [...source.orders, { ...source.orders[0], environment: 'TEST' }] }).series).toBeNull();
  });
});
