import { useEffect, useRef, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { checkoutApi, checkoutMoney, paymentPresentation, redirectToPayment } from '../services/checkoutApi';
import { useStore } from '../contexts/StoreContext';
import { Button, PageHeader } from '../components/storefront/UI';

export default function PaymentReturn() {
  const [params, setParams] = useSearchParams();
  const id = params.get('paymentId');
  const { cart } = useStore();
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
        const result = await checkoutApi.payment(id, controller.signal);
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
      const result = await checkoutApi.retry(payment.orderId, retryKey.current);
      retryKey.current = null;
      if (result.redirectUrl) redirectToPayment(result.redirectUrl);
      else { setParams({ paymentId: String(result.paymentId) }); setRevision(v => v + 1); }
    } catch (cause) { setError(cause); }
    finally { lock.current = false; setPending(false); }
  };
  const state = paymentPresentation(payment);
  return <div className="s-container s-page">
    <PageHeader title={!id || !/^\d+$/.test(id) ? 'Payment reference missing' : state.title} />
    <div className="s-panel" aria-live="polite" role="status"><p>{state.detail}</p>{payment?.amount != null && <p>{checkoutMoney(payment.amount, payment.currency)}</p>}</div>
    {error && <p role="alert" className="s-error-text">{error.response?.status === 404 ? 'This payment could not be found in your account.' : error.response?.data?.title || 'We could not verify this payment yet. You can check again or open your orders.'}</p>}
    <div className="s-actions">{payment?.orderId && <Link className="s-button s-button-secondary" to={`/orders/${payment.orderId}`}>View order</Link>}{payment?.redirectUrl && state.pending && <Button disabled={pending} onClick={() => { try { redirectToPayment(payment.redirectUrl); } catch (cause) { setError(cause); } }}>Continue secure payment</Button>}{state.retry && <Button disabled={pending} onClick={retry}>{pending ? 'Starting secure payment…' : 'Retry secure payment'}</Button>}{state.pending && id && <Button variant="secondary" onClick={() => setRevision(v => v + 1)}>Check again</Button>}<Link to="/orders" className="s-text-link">Your orders</Link><Link to="/cart" className="s-text-link">Your cart</Link></div>
  </div>;
}
