import CheckoutTotals from '@/features/checkout/components/CheckoutTotals.jsx';
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';

export default function CheckoutReview({ quote }) {
  return (

      <aside className="s-summary" aria-label="Order review">
        <h2>Your order</h2>
        {quote.data ? <>{quote.data.items.map(i => <div className="s-summary-item" key={i.cartItemId}><div><strong>{i.name}</strong><p>{i.quantity} × {checkoutMoney(i.unitPrice, quote.data.currency)}</p>{i.itemType === 'CustomDesign' && <small>Custom design specifications are preserved with your order.</small>}</div></div>)}<CheckoutTotals quote={quote.data} /><p>Currency: {quote.data.currency}</p></> : <p>Totals appear after your server quote is ready.</p>}
      </aside>
  );
}
