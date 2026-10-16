import { checkoutApi } from '@/features/checkout/checkoutApi.js';
import { useCheckoutPayment } from '@/features/payments/hooks/useCheckoutPayment.js';
import { useState } from 'react';
import { useResource } from '@/shared/hooks/useResource.js';
import { addressApi } from '@/features/account/addressApi.js';

export function useCheckout(cartVersion = '') {
  const addresses = useResource(addressApi.getAddresses, 'checkout-addresses');
  const [addressId, setAddressId] = useState('');
  const [shippingMethodId, setShippingMethodId] = useState('');
  const [couponCode, setCouponCode] = useState('');
  const selectedAddress = addressId || String(addresses.data?.find(a => a.isDefault)?.id || addresses.data?.[0]?.id || '');
  const request = { addressId: Number(selectedAddress), shippingMethodId: shippingMethodId ? Number(shippingMethodId) : null, couponCode: couponCode.trim() || null };
  const key = JSON.stringify([request, cartVersion]);
  const quote = useResource(signal => checkoutApi.quote(request, signal), key, Boolean(selectedAddress));
  const payment = useCheckoutPayment(request, key, quote);
  const { pending, error, pay, session, setError, setSession } = payment;
  const chooseAddress = value => { setAddressId(value); setShippingMethodId(''); setError(null); setSession(null); };
  return { addresses, selectedAddress, chooseAddress, shippingMethodId, setShippingMethodId, couponCode, setCouponCode, quote, pending, error, pay, session };
}
