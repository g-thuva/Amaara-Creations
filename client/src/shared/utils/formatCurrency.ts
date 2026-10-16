export const formatCurrency = (value: number, currency = 'LKR') => currency === 'LKR'
  ? `Rs. ${new Intl.NumberFormat('en-LK', { minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(Number(value) || 0)}`
  : new Intl.NumberFormat('en-CH', { style: 'currency', currency, minimumFractionDigits: 2, maximumFractionDigits: 2 }).format(Number(value) || 0);

// ISO-labelled display used for checkout and operational totals; never converts money.
export function formatIsoCurrency(value: number, currency = 'LKR') {
  return new Intl.NumberFormat('en', { style: 'currency', currency }).format(Number(value));
}
