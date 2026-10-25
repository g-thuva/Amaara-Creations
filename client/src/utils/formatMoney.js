import { STORE_CURRENCY } from '@/shared/config/currency.js';
const decimals = new Intl.NumberFormat('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 });
export function formatMoney(value) {
  return Number.isFinite(value) ? `${STORE_CURRENCY} ${decimals.format(value)}` : '—';
}
export function abbreviateNumber(value) {
  if (!Number.isFinite(value)) return '—';
  const magnitude = Math.abs(value);
  const [divisor, suffix] = magnitude >= 1e9 ? [1e9, 'B'] : magnitude >= 1e6 ? [1e6, 'M'] : magnitude >= 1e3 ? [1e3, 'K'] : [1, ''];
  return new Intl.NumberFormat('en-GB', { maximumFractionDigits: suffix ? 1 : 0 }).format(value / divisor) + suffix;
}
export function abbreviateMoney(value) {
  return Number.isFinite(value) ? `${STORE_CURRENCY} ${abbreviateNumber(value)}` : '—';
}
