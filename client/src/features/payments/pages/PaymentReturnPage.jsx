import { Link } from 'react-router-dom';
import { usePaymentReturn } from '@/features/payments/hooks/usePaymentReturn.js';
import { paymentPresentation, redirectToPayment } from '@/features/payments/paymentNavigation.js';
import { formatIsoCurrency as checkoutMoney } from '@/shared/utils/formatCurrency.ts';
import { Button } from '@/shared/components/ui/Button.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';

export default function PaymentReturnPage() {
  const { id, payment, error, pending, retry, setError, refresh } = usePaymentReturn();
  const state = paymentPresentation(payment);
  return <div className="s-container s-page">
    <PageHeader title={!id || !/^\d+$/.test(id) ? 'Payment reference missing' : state.title} />
    <div className="s-panel" aria-live="polite" role="status"><p>{state.detail}</p>{payment?.amount != null && <p>{checkoutMoney(payment.amount, payment.currency)}</p>}</div>
    {error && <p role="alert" className="s-error-text">{error.response?.status === 404 ? 'This payment could not be found in your account.' : error.response?.data?.title || 'We could not verify this payment yet. You can check again or open your orders.'}</p>}
    <div className="s-actions">{payment?.orderId && <Link className="s-button s-button-secondary" to={`/orders/${payment.orderId}`}>View order</Link>}{payment?.redirectUrl && state.pending && <Button disabled={pending} onClick={() => { try { redirectToPayment(payment.redirectUrl); } catch (cause) { setError(cause); } }}>Continue secure payment</Button>}{state.retry && <Button disabled={pending} onClick={retry}>{pending ? 'Starting secure payment…' : 'Retry secure payment'}</Button>}{state.pending && id && <Button variant="secondary" onClick={refresh}>Check again</Button>}<Link to="/orders" className="s-text-link">Your orders</Link><Link to="/cart" className="s-text-link">Your cart</Link></div>
  </div>;
}
