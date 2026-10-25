import { STORE_CURRENCY } from '@/shared/config/currency.js';
import { formatCurrency } from '@/shared/utils/formatCurrency.ts';
export function Price({ value, currency = STORE_CURRENCY }) {
  return <span className="s-price">{formatCurrency(value, currency)}</span>;
}
