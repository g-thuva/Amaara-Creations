import { STORE_CURRENCY } from '@/shared/config/currency.js';
export const formatCurrency = (value: number, currency = STORE_CURRENCY) => currency === 'LKR'
  ? `Rs. ${new Intl.NumberFormat('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(Number(value) || 0)}`
  : new Intl.NumberFormat('en-CH', { style: 'currency', currency, minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(Number(value) || 0);

// ISO-labelled display used for checkout and operational totals; never converts money.
export function formatIsoCurrency(value: number, currency = STORE_CURRENCY) {
  return new Intl.NumberFormat('en', { style: 'currency', currency }).format(Number(value));
}
