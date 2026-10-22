import { useState } from 'react';
import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';

export default function CheckoutForm({ checkout }) {
  const [couponInput, setCouponInput] = useState('');
  const [notes, setNotes] = useState('');
  const { addresses, selectedAddress, quote, pending } = checkout;
  const selected = addresses.data?.find(a => String(a.id) === selectedAddress);
  return (
      <form className="s-form s-checkout-form" onSubmit={event => { event.preventDefault(); checkout.pay(notes); }} aria-busy={pending}>
        <h2>1. Delivery address</h2>
        <label>Saved address<select value={selectedAddress} onChange={e => checkout.chooseAddress(e.target.value)} disabled={pending}>
          {addresses.data.map(a => <option key={a.id} value={a.id}>{a.label} — {a.recipientName}, {a.city}</option>)}
        </select></label>
        {selected && <address>{selected.recipientName}<br />{selected.addressLine1} {selected.addressLine2}<br />{selected.city} {selected.postalCode}<br />{selected.country}</address>}
        <Link to="/addresses" className="s-text-link">Manage delivery addresses</Link>
        <h2>2. Delivery option</h2>
        {quote.loading ? <p role="status">Calculating your checkout quote…</p> : quote.error ? <><ErrorState error={quote.error} retry={quote.reload} /><Button type="button" variant="secondary" onClick={() => checkout.setShippingMethodId('')}>Choose another delivery option</Button></> : <>
          <label>Shipping method<select value={checkout.shippingMethodId} onChange={e => checkout.setShippingMethodId(e.target.value)} disabled={pending}>
            <option value="">Choose delivery</option>
            {quote.data?.availableShippingMethods.map(m => <option key={m.id} value={m.id}>{m.name} — {checkoutMoney(m.amount, m.currency)}</option>)}
          </select></label>
          {quote.data && !quote.data.availableShippingMethods.length && <p role="alert">No delivery option is available for this address. Update your address or contact Amaara.</p>}
          {quote.data?.availableShippingMethods.filter(m => String(m.id) === checkout.shippingMethodId).map(m => <p key={m.id}>{m.isPickup ? 'Pickup: ' + m.pickupLocation : m.estimatedMinimumDays != null ? 'Estimated delivery: ' + m.estimatedMinimumDays + '–' + (m.estimatedMaximumDays ?? m.estimatedMinimumDays) + ' days.' : 'Delivery estimate will be confirmed by Amaara.'}{m.isDevelopmentValue && ' Development rate — business owner verification required.'}</p>)}
        </>}
        <h2>3. Coupon</h2>
        <label>Coupon code<input value={couponInput} onChange={e => setCouponInput(e.target.value)} maxLength="40" disabled={pending} aria-describedby={quote.data?.couponResult ? 'coupon-feedback' : undefined} /></label>
        <div className="s-actions"><Button type="button" variant="secondary" disabled={pending || quote.loading} onClick={() => checkout.setCouponCode(couponInput)}>Apply coupon</Button>{checkout.couponCode && <Button type="button" variant="secondary" disabled={pending} onClick={() => { checkout.setCouponCode(''); setCouponInput(''); }}>Remove coupon</Button>}</div>
        {quote.data?.couponResult && <p id="coupon-feedback" role={quote.data.couponResult.applied ? 'status' : 'alert'}>{quote.data.couponResult.message}</p>}
        <h2>4. Review and pay</h2>
        <label>Order notes (optional)<textarea rows="3" value={notes} onChange={e => setNotes(e.target.value)} maxLength="500" disabled={pending} /></label>
        {quote.data?.paymentAvailable && quote.data.paymentProvider === 'Zahls' && quote.data.paymentMode === 'TEST' &&
          <p role="status" className="s-test-payment-banner">Test payment — no real money will be charged.</p>}
        {quote.data?.paymentAvailable
          ? <p>Payment takes place on the payment provider’s hosted page. Your order is confirmed after our server verifies payment.</p>
          : quote.data && <p role="status">Secure online payment is not available for this checkout yet. Your cart has not been charged.</p>}
        {checkout.error && <p role="alert" className="s-error-text">{checkout.error.response?.data?.title || 'Payment could not be started. Check your connection and try again.'}</p>}
        {checkout.session && !checkout.session.redirectUrl && <p role="status">This payment is {checkout.session.status.toLowerCase()}. <Link to={'/checkout/payment/return?paymentId=' + checkout.session.paymentId}>Check payment status</Link></p>}
        <div className="s-actions"><Link to="/cart" className="s-text-link">Back to cart</Link><Button type="submit" disabled={pending || quote.loading || !quote.data?.canPay}>{pending ? 'Starting secure payment…' : 'Pay securely'}</Button></div>
        {pending && <p role="status">Preparing your order and opening secure payment…</p>}
      </form>
  );
}
