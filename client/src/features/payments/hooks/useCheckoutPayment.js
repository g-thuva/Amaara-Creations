import { useRef, useState } from 'react';
import { checkoutApi } from '@/features/checkout/checkoutApi.js';
import { redirectToPayment } from '@/features/payments/paymentNavigation.js';

// Session creation is atomic on the server. Keep the key through ambiguous failures.
export function useCheckoutPayment(request, key, quote) {
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

  return { pending, error, pay, session, setError, setSession };
}
