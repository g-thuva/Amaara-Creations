import { STORE_CURRENCY } from '@/shared/config/currency.js';
// Presentation only. Preserve the supplied currency and UTC instant.
export function formatIsoCurrency(value, currency = STORE_CURRENCY) {
  const number = Number(value);
  return `${currency} ${Number.isFinite(number) ? new Intl.NumberFormat('en-GB', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(number) : '—'}`;
}
export const formatCurrency = formatIsoCurrency;
function instant(value) {
  if (!value) return null;
  const input = typeof value === 'string' && /^\d{4}-\d\d-\d\dT/.test(value) && !/(Z|[+-]\d\d:\d\d)$/i.test(value) ? value + 'Z' : value;
  const date = new Date(input);
  return Number.isNaN(date.getTime()) ? null : date;
}
export function formatLocalDate(value) {
  const date = instant(value);
  return date ? new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Colombo', day: '2-digit', month: '2-digit', year: 'numeric' }).format(date) : '—';
}
export const formatDate = formatLocalDate;
export function formatDateTime(value) {
  const date = instant(value);
  return date ? new Intl.DateTimeFormat('en-GB', { timeZone: 'Asia/Colombo', day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit', hour12: false }).format(date) : '—';
}
