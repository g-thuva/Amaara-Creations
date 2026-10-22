import { Link } from 'react-router-dom';
import Icon from '@/shared/components/ui/Icon.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';
export default function CartSummary({ cart, invalid, currencyMismatch }) {
  return (
          <aside className="s-summary">
            <h2>Order summary</h2>
            <div className="s-row"><span>{cart.data.totalItems} {cart.data.totalItems === 1 ? 'item' : 'items'}</span>{cart.data.hasMixedCurrencies ? <strong>Mixed currencies</strong> : <Price value={cart.data.total} currency={cart.data.currency} />}</div>
            <strong className="s-summary-label">Subtotal</strong>
            <p>Shipping and payment arrangements are not included in this item subtotal.</p>
            {currencyMismatch && <p className="s-error-text" role="alert">Remove items priced in another currency before checkout.</p>}
            {invalid ? <p className="s-error-text">Resolve unavailable items before continuing.</p> : currencyMismatch ? null : <Link className="s-button s-button-primary" to="/checkout">Continue to checkout <Icon name="arrow" /></Link>}
          </aside>
  );
}
