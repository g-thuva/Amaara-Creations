import { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useStore } from '../contexts/StoreContext';
import { orderApi } from '../services/orderApi';
import { Button, EmptyState, ErrorState, Media, PageHeader, Price, Skeleton } from '../components/storefront/UI';
export default function Checkout() {
  const { cart, pending, mutate } = useStore();
  const navigate = useNavigate();
  const [placed, setPlaced] = useState(false);
  const invalid = cart.data?.items.some(i => i.isOutOfStock || i.quantity > i.productStock);
  const submit = async e => {
    e.preventDefault(); if (placed || invalid) return;
    const form = Object.fromEntries(new FormData(e.currentTarget));
    await mutate(async () => { const order = await orderApi.createOrder(form); setPlaced(true); cart.reload(); navigate(`/orders/${order.id}`, { replace: true, state: { message: 'Your order has been placed. No online payment has been collected.' } }); }, null);
  };
  return <div className="s-container s-page"><PageHeader eyebrow="One last look" title="Checkout">Review your items and delivery details.</PageHeader>{cart.loading ? <Skeleton count={2}/> : cart.error ? <ErrorState error={cart.error} retry={cart.reload}/> : !cart.data?.items.length ? <EmptyState title="Your cart is empty"/> : <div className="s-commerce-layout"><form className="s-form" onSubmit={submit}><h2>Delivery details</h2><p className="s-alert">This checkout records your order. Online payment and shipping quotes are not available yet.</p><label>Address<textarea name="shippingAddress" autoComplete="street-address" required maxLength="500" rows="3"/></label><label>City<input name="shippingCity" autoComplete="address-level2" maxLength="100" required/></label><label>Postal code<input name="shippingPostalCode" autoComplete="postal-code" maxLength="50" required/></label><label>Country<input name="shippingCountry" autoComplete="country-name" defaultValue="Sri Lanka" maxLength="100" required/></label><label>Order notes (optional)<textarea name="notes" rows="3" maxLength="500"/></label>{invalid && <p className="s-error-text" role="alert">Some items are no longer available in this quantity. Please update your cart.</p>}<div className="s-actions"><Link to="/cart" className="s-text-link">Back to cart</Link><Button type="submit" disabled={pending || placed || invalid}>{pending ? 'Placing order…' : 'Place order'}</Button></div></form><aside className="s-summary"><h2>Your order</h2>{cart.data.items.map(item => <div className="s-summary-item" key={item.id}><Media src={item.productImageUrl} alt={item.productName}/><div><strong>{item.productName}</strong><p>{item.quantity} × <Price value={item.productPrice}/></p></div></div>)}<div className="s-row"><strong>Product subtotal</strong><Price value={cart.data.total}/></div><p>Shipping and payment arrangements are not included.</p></aside></div>}</div>;
}
