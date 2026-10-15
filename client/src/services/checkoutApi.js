import api from './api';

export const checkoutApi = {
  quote: async (request, signal) => (await api.post('/checkout/quote', request, { signal })).data,
  session: async (request) => (await api.post('/checkout/session', request)).data,
  payment: async (id, signal) => (await api.get(`/payments/${id}`, { signal })).data,
  retry: async (orderId, idempotencyKey) => (await api.post(`/checkout/orders/${orderId}/retry`, { idempotencyKey })).data,
};

// Follow only HTTPS links on the provider's payment hosts. The URL itself comes
// from the server-side Gateway response; the browser never constructs it.
export function redirectToPayment(url, navigate = (target) => window.location.assign(target)) {
  const target = new URL(url);
  const zahlsHost = target.hostname === 'pay.zahls.ch' ||
    (/^[a-z0-9][a-z0-9-]*\.zahls\.ch$/.test(target.hostname) && target.pathname === '/pay');
  const legacyDatatrans = target.hostname === 'pay.sandbox.datatrans.com' &&
    /^\/v1\/start\/[0-9]{10,30}$/.test(target.pathname) && !target.search;
  if (target.protocol !== 'https:' || target.port || target.username || target.password ||
    target.hash || !(zahlsHost || legacyDatatrans)) {
    throw new Error('The secure payment address could not be verified.');
  }
  navigate(target.href);
}

export function paymentPresentation(payment) {
  if (!payment) return { title: 'Verifying your payment', detail: 'Checking the payment securely with our server.', pending: true };
  if (payment.orderStatus === 'PaymentReview') return { title: 'Payment received; order under review', detail: 'Amaara needs to review this order before fulfilment. Open your order for updates.' };
  if (['Paid', 'PartiallyRefunded', 'Refunded'].includes(payment.status)) return { title: 'Payment confirmed', detail: 'Your payment has been verified by our server.', confirmed: true };
  if (payment.status === 'Failed') return { title: 'Payment failed', detail: 'Your cart is preserved. You can retry payment from this order.', retry: true };
  if (payment.status === 'Cancelled') return { title: 'Payment cancelled', detail: 'Your cart is preserved. You can retry payment from this order.', retry: true };
  if (payment.unableToVerify) return { title: 'Unable to verify yet', detail: 'We will continue checking. Open your order before starting another payment.', pending: true };
  return { title: 'Payment processing', detail: payment.status === 'Authorized' ? 'The payment is authorized and awaiting settlement.' : 'We are waiting for the provider confirmation.', pending: true };
}

export function checkoutMoney(value, currency = 'LKR') {
  return new Intl.NumberFormat('en', { style: 'currency', currency }).format(Number(value));
}
