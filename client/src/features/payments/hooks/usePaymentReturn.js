import { useEffect, useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import { paymentsApi } from '@/features/payments/paymentsApi.js';
import { redirectToPayment } from '@/features/payments/paymentNavigation.js';
import { useCart } from '@/features/cart/CartProvider.jsx';
export function usePaymentReturn() {
  const [params, setParams] = useSearchParams();
  const id = params.get('paymentId');
  const { cart } = useCart();
  const [payment, setPayment] = useState(null);
  const [error, setError] = useState(null);
  const [pending, setPending] = useState(false);
  const [revision, setRevision] = useState(0);
  const lock = useRef(false);
  const retryKey = useRef(null);
  const refreshed = useRef(null);
  const reloadCart = cart.reload;
  useEffect(() => {
    if (!id || !/^\d+$/.test(id)) return;
    let active = true; let timer; let polls = 0;
    const controller = new AbortController();
    setPayment(null); setError(null);
    const check = async () => {
      try {
        const result = await paymentsApi.payment(id, controller.signal);
        if (!active) return;
        setPayment(result); setError(null);
        if (['Paid', 'PartiallyRefunded', 'Refunded'].includes(result.status)) {
          if (refreshed.current !== id) { refreshed.current = id; reloadCart(); }
        } else if (!['Failed', 'Cancelled'].includes(result.status) && ++polls < 40) timer = setTimeout(check, 3000);
      } catch (cause) {
        if (!active || cause.code === 'ERR_CANCELED') return;
        setError(cause);
        if (cause.response?.status !== 404 && cause.response?.status !== 401 && ++polls < 40) timer = setTimeout(check, 5000);
      }
    };
    check();
    return () => { active = false; controller.abort(); clearTimeout(timer); };
  }, [id, revision, reloadCart]);
  const retry = async () => {
    if (lock.current || !payment) return;
    lock.current = true; setPending(true); setError(null);
    retryKey.current ||= crypto.randomUUID();
    try {
      const result = await paymentsApi.retry(payment.orderId, retryKey.current);
      retryKey.current = null;
      if (result.redirectUrl) redirectToPayment(result.redirectUrl);
      else { setParams({ paymentId: String(result.paymentId) }); setRevision(v => v + 1); }
    } catch (cause) { setError(cause); }
    finally { lock.current = false; setPending(false); }
  };

  return { id, payment, error, pending, retry, setError, refresh: () => setRevision(v => v + 1) };
}
