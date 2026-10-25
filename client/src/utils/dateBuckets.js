export const COLOMBO = 'Asia/Colombo';
const DAY = 86400000;
const OFFSET = 19800000;
/** API DateTime values without an offset are stored as UTC. */
export function utcInstant(value) {
  if (value == null || value === '') return null;
  const normalized = typeof value === 'string' && /^\d{4}-\d\d-\d\dT/.test(value) && !/(Z|[+-]\d\d:\d\d)$/i.test(value) ? value + 'Z' : value;
  const date = new Date(normalized);
  return Number.isNaN(date.getTime()) ? null : date;
}
export function colomboDay(value) {
  const date = utcInstant(value);
  if (!date) return null;
  const parts = new Intl.DateTimeFormat('en-GB', { timeZone: COLOMBO, year: 'numeric', month: '2-digit', day: '2-digit' }).formatToParts(date);
  const get = type => parts.find(part => part.type === type).value;
  return `${get('year')}-${get('month')}-${get('day')}`;
}
export function colomboMidnight(day) {
  return new Date(Date.parse(`${day}T00:00:00Z`) - OFFSET);
}
/** Calendar-day periods, ending at tomorrow's Colombo midnight (exclusive).
 * 3M and 12M include the current month to date and the preceding 2/11 months.
 * The comparison always has the exact same number of calendar days.
 */
export function dashboardPeriod(period = '30D', now = new Date()) {
  const today = colomboDay(now);
  const end = new Date(colomboMidnight(today).getTime() + DAY);
  const [year, month] = today.split('-').map(Number);
  const start = period === '3M' || period === '12M'
    ? new Date(Date.UTC(year, month - (period === '3M' ? 3 : 12), 1) - OFFSET)
    : new Date(end.getTime() - (period === '7D' ? 7 : 30) * DAY);
  const days = Math.round((end - start) / DAY);
  return { period, start: start.toISOString(), end: end.toISOString(), previousStart: new Date(start.getTime() - days * DAY).toISOString(), days, monthly: period.endsWith('M') };
}
/** @returns {string[]} */
export function periodDays(range) {
  return Array.from({ length: range.days }, (_, index) => /** @type {string} */ (colomboDay(new Date(Date.parse(range.start) + index * DAY))));
}
/** Missing intervals are zero only when the caller has a COMPLETE scoped list. */
export function bucketOrders(orders, range) {
  const buckets = new Map(periodDays(range).map(date => [date, { date, value: 0, orders: 0 }]));
  for (const order of orders) {
    const bucket = buckets.get(colomboDay(order.orderDate));
    if (bucket && Number.isFinite(order.total)) { bucket.value += order.total; bucket.orders++; }
  }
  return [...buckets.values()];
}
export function monthlyBuckets(days) {
  const months = new Map();
  for (const day of days) {
    const key = day.date.slice(0, 7);
    const bucket = months.get(key) || { date: key + '-01', value: 0, orders: 0 };
    bucket.value += day.value; bucket.orders += day.orders; months.set(key, bucket);
  }
  return [...months.values()];
}
export function periodLabel(range) {
  const format = value => new Intl.DateTimeFormat('en-GB', { timeZone: COLOMBO, day: '2-digit', month: '2-digit', year: 'numeric' }).format(new Date(value));
  return `${format(range.start)} – ${format(new Date(Date.parse(range.end) - 1))}`;
}
