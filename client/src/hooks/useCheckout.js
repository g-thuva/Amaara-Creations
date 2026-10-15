import { useRef, useState } from 'react';
import { useResource } from './useResource';
import { addressApi } from '../services/addressApi';
import { checkoutApi, redirectToPayment } from '../services/checkoutApi';

export function useCheckout(cartVersion = '') {
  const addresses = useResource(addressApi.getAddresses, 'checkout-addresses');
  const [addressId, setAddressId] = useState('');
  const [shippingMethodId, setShippingMethodId] = useState('');
  const [couponCode, setCouponCode] = useState('');
  const selectedAddress = addressId || String(addresses.data?.find(a => a.isDefault)?.id || addresses.data?.[0]?.id || '');
  const request = { addressId: Number(selectedAddress), shippingMethodId: shippingMethodId ? Number(shippingMethodId) : null, couponCode: couponCode.trim() || null };
  const key = JSON.stringify([request, cartVersion]);
  const quote = useResource(signal => checkoutApi.quote(request, signal), key, Boolean(selectedAddress));
  const attempt = useRef(null);
  const locked = useRef(false);
  const [pending, setPending] = useState(false);
  const [error, setError] = useState(null);
  const [session, setSession] = useState(null);
  const pay = async (notes = '') => {
    if (locked.current || quote.loading || !quote.data?.canPay) return;
    locked.current = true; setPending(true); setError(null);
    const identity = JSON.stringify([key, quote.data.quoteVersion, notes]);
    if (attempt.current?.identity !== identity) attempt.current = { identity, id: crypto.randomUUID() };
    try {
      const payment = await checkoutApi.session({ ...request, notes, quoteVersion: quote.data.quoteVersion, idempotencyKey: attempt.current.id });
      setSession(payment);
      if (payment.redirectUrl) redirectToPayment(payment.redirectUrl);
    } catch (cause) {
      setError(cause);
      if (cause.code === 'CHECKOUT_CHANGED' || cause.response?.data?.code === 'CHECKOUT_CHANGED') { attempt.current = null; quote.reload(); }
    } finally { locked.current = false; setPending(false); }
  };
  const chooseAddress = value => { setAddressId(value); setShippingMethodId(''); setError(null); setSession(null); };
  return { addresses, selectedAddress, chooseAddress, shippingMethodId, setShippingMethodId, couponCode, setCouponCode, quote, pending, error, pay, session };
}
