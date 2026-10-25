import { describe, expect, it } from 'vitest';
import { formatMoney, abbreviateMoney, abbreviateNumber } from './formatMoney.js';
import { colomboDay, colomboMidnight, dashboardPeriod, bucketOrders, monthlyBuckets, periodLabel } from './dateBuckets.js';
describe('LKR presentation', () => {
  it('uses ISO code, grouping and two decimals', () => { expect(formatMoney(1234.5)).toBe('LKR 1,234.50'); expect(formatMoney(0)).toBe('LKR 0.00'); expect(formatMoney(null)).toBe('—'); });
  it.each([[50000, '50K'], [1500000, '1.5M'], [0, '0'], [123, '123'], [1250000000, '1.3B']])('abbreviates %s as %s', (value, result) => expect(abbreviateNumber(value)).toBe(result));
  it('labels abbreviated money', () => expect(abbreviateMoney(184500)).toBe('LKR 184.5K'));
});
describe('Colombo calendar boundaries', () => {
  it('changes day at 18:30 UTC, including an offset-less API DateTime', () => {
    expect(colomboDay('2026-10-22T18:29:59Z')).toBe('2026-10-22'); expect(colomboDay('2026-10-22T18:30:00Z')).toBe('2026-10-23'); expect(colomboDay('2026-10-22T18:30:00')).toBe('2026-10-23'); expect(colomboDay('invalid')).toBeNull();
    expect(colomboMidnight('2026-10-23').toISOString()).toBe('2026-10-22T18:30:00.000Z');
  });
  it('uses an immediately preceding equal-length period', () => {
    const range = dashboardPeriod('30D', new Date('2026-10-23T05:00:00Z'));
    expect(periodLabel(range)).toBe('24/09/2026 – 23/10/2026'); expect(range.days).toBe(30);
    expect(Date.parse(range.end) - Date.parse(range.start)).toBe(Date.parse(range.start) - Date.parse(range.previousStart));
    expect(range.previousStart).toBe('2026-08-24T18:30:00.000Z');
  });
  it.each(['3M', '12M'])('keeps %s comparison equally long through leap/month boundaries', period => {
    const range = dashboardPeriod(period, new Date('2028-03-01T05:00:00Z'));
    expect(Date.parse(range.end) - Date.parse(range.start)).toBe(Date.parse(range.start) - Date.parse(range.previousStart));
  });
  it('buckets complete orders without losing midnight orders', () => {
    const range = dashboardPeriod('7D', new Date('2026-10-23T05:00:00Z'));
    const data = bucketOrders([{ orderDate: '2026-10-22T18:29:59Z', total: 10 }, { orderDate: '2026-10-22T18:30:00', total: 20 }, { orderDate: range.end, total: 100 }], range);
    expect(data).toHaveLength(7); expect(data.at(-2)).toEqual({ date: '2026-10-22', value: 10, orders: 1 }); expect(data.at(-1)).toEqual({ date: '2026-10-23', value: 20, orders: 1 }); expect(monthlyBuckets(data)).toEqual([{ date: '2026-10-01', value: 30, orders: 2 }]);
  });
});
