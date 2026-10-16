import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';

export default function CheckoutTotals({ quote }) {
  const rows = [['Item subtotal', quote.subtotal], ['Discount', -quote.discount], ['Shipping', quote.shipping], [quote.pricesIncludeTax ? 'Tax (included)' : 'Tax', quote.tax], ['Payment fee', quote.paymentFee], ['Total', quote.total]];
  return <dl aria-label="Server checkout totals">{rows.map(([label, value]) => <div className="s-row" key={label}><dt>{label}</dt><dd>{checkoutMoney(value, quote.currency)}</dd></div>)}</dl>;
}
