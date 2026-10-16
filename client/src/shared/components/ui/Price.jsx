import { formatCurrency } from '@/shared/utils/formatCurrency.ts';
export function Price({ value, currency = "LKR" }) {
  return <span className="s-price">{formatCurrency(value, currency)}</span>;
}
