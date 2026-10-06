import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useStore } from '../contexts/StoreContext';
import { orderApi } from '../services/orderApi';
import { Button, EmptyState, ErrorState, Icon, Media, PageHeader, Price, Skeleton } from '../components/storefront/UI';

export default function Checkout() {
  const { cart, pending, mutate } = useStore();
  const navigate = useNavigate();
  const [placed, setPlaced] = useState(false);
  const invalid = cart.data?.items.some((item) => item.isOutOfStock || item.quantity > item.productStock);

  const submit = async (event) => {
    event.preventDefault();
    if (placed || invalid) return;
    const form = Object.fromEntries(new FormData(event.currentTarget));
    await mutate(async () => {
      const order = await orderApi.createOrder(form);
      setPlaced(true);
      cart.reload();
      navigate(`/orders/${order.id}`, { replace: true, state: { message: 'Your order has been placed. No online payment has been collected.' } });
    }, null);
  };

  return (
    <div className="s-container s-page">
      <PageHeader eyebrow="Order details" title="Checkout">Review the products and enter a delivery address.</PageHeader>
      {cart.loading ? <Skeleton count={2} /> : cart.error ? <ErrorState error={cart.error} retry={cart.reload} /> : !cart.data?.items.length ? <EmptyState title="Your cart is empty" /> : (
        <div className="s-commerce-layout">
          <form className="s-form s-checkout-form" onSubmit={submit}>
            <h2>Delivery details</h2>
            <p className="s-alert"><Icon name="check" />This checkout records your order. Online payment and shipping quotes are not available yet.</p>
            <label>Address<textarea name="shippingAddress" autoComplete="street-address" required maxLength="500" rows="3" /></label>
            <label>City<input name="shippingCity" autoComplete="address-level2" maxLength="100" required /></label>
            <label>Postal code<input name="shippingPostalCode" autoComplete="postal-code" maxLength="50" required /></label>
            <label>Country<input name="shippingCountry" autoComplete="country-name" defaultValue="Sri Lanka" maxLength="100" required /></label>
            <label>Order notes (optional)<textarea name="notes" rows="3" maxLength="500" /></label>
            {invalid && <p className="s-error-text" role="alert">Some items are no longer available in this quantity. Please update your cart.</p>}
            <div className="s-actions"><Link to="/cart" className="s-text-link"><Icon name="arrowLeft" />Back to cart</Link><Button type="submit" disabled={pending || placed || invalid}>{pending ? 'Placing order…' : 'Place order'}</Button></div>
          </form>
          <aside className="s-summary">
            <h2>Your order</h2>
            {cart.data.items.map((item) => (
              <div className="s-summary-item" key={item.id}>
                <Media src={item.productImageUrl} alt={item.productName} width={160} height={120} />
                <div><strong>{item.productName}</strong>{item.itemType === 'CustomDesign' && <small>Custom · {item.width} × {item.height} {item.measurementUnit || 'cm'}</small>}<p>{item.quantity} × <Price value={item.productPrice} /></p></div>
              </div>
            ))}
            <div className="s-row"><strong>Item subtotal</strong><Price value={cart.data.total} /></div>
            <p>Shipping and payment arrangements are not included.</p>
          </aside>
        </div>
      )}
    </div>
  );
}
