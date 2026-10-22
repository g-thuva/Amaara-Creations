import { test, type Page } from '@playwright/test';
// UI-only payment-state fixtures, including mocked session transport. They do not
// establish authentication/authorization evidence; management authentication specs
// use the real Identity/JWT API and disposable SQL-backed/in-memory identities.
export async function checkoutFixture(page: Page, { mixed = false, admin = false, paymentAvailable = true } = {}) {
  const frontend = String(test.info().project.use.baseURL);
  const state = { status: 'Pending', orderStatus: 'PendingPayment', sessions: [] as Record<string, unknown>[], retries: 0, refunds: [] as Record<string, unknown>[], cartReads: 0, quotes: [] as Record<string, unknown>[] };
  const line = { cartItemId: 1, itemType: 'Product', name: 'Fixture standard label', quantity: 2, unitPrice: 100, subtotal: 200, currency: 'CHF' };
  const items = mixed ? [line, { cartItemId: 2, itemType: 'CustomDesign', name: 'Fixture custom sticker', quantity: 4, unitPrice: 12, subtotal: 48, currency: 'CHF' }] : [line];
  const subtotal = mixed ? 248 : 200;
  const method = { id: 1, code: 'TEST_DELIVERY', name: 'Fixture delivery', amount: 25, currency: 'CHF', estimatedMinimumDays: 2, estimatedMaximumDays: 4, isPickup: false, isDevelopmentValue: true };
  const payment = () => ({ id: 1, orderId: 1, provider: 'Zahls', status: state.status, orderStatus: state.orderStatus, amount: subtotal + 25, currency: 'CHF', settledAmount: state.status === 'Paid' ? subtotal + 25 : 0, authorizedAmount: 0, refundedAmount: 0, refundableAmount: state.status === 'Paid' ? subtotal + 25 : 0, providerReference: 'TEST-ONLY', providerTransactionId: '260000000000000001', refunds: state.refunds, expiresAt: new Date(Date.now() + 600000).toISOString() });
  const order = () => ({ id: 1, orderNumber: 'FIXTURE-ONLY-1', orderDate: '2026-10-11T12:00:00Z', status: state.orderStatus, paymentStatus: state.status, fulfilmentStatus: 'NotReady', currency: 'CHF', total: subtotal + 25, subtotal, shippingTotal: 25, taxTotal: 0, discountTotal: 0, paymentFee: 0, shippingMethodName: method.name, userName: 'Fixture customer', orderItems: items.map((i, n) => ({ ...i, id: n + 1, productName: i.name, price: i.unitPrice })), payments: [{ id: 1 }], shipments: [], history: [{ newStatus: 'PendingPayment', reason: 'Test fixture only' }] });
  await page.route('**/api/v1/**', async route => {
    const request = route.request(); const path = new URL(request.url()).pathname.replace('/api/v1', '');
    const headers = { 'Access-Control-Allow-Origin': new URL(frontend).origin, 'Access-Control-Allow-Credentials': 'true', 'Access-Control-Allow-Headers': 'authorization,content-type', 'Access-Control-Allow-Methods': 'GET,POST,PUT,DELETE,OPTIONS' };
    const reply = (body: unknown, status = 200) => route.fulfill({ status, headers, contentType: 'application/json', body: JSON.stringify(body) });
    if (request.method() === 'OPTIONS') return route.fulfill({ status: 204, headers });
    if (path === '/admin/capabilities') return reply({}, admin ? 200 : 403);
    if (path === '/auth/staff/refresh') return reply({ token: 'fixture-only-token', user: { id: 'fixture', name: 'Fixture staff', email: 'fixture@example.test', roles: ['Admin'] } }, admin ? 200 : 401);
    if (path === '/auth/staff/session') return reply({ status: 'authorized', user: { id: 'fixture', name: 'Fixture staff', email: 'fixture@example.test', roles: ['Admin'] }, permissions: ['AdminAccess', 'ViewOrders', 'ManageOrders', 'ManagePayments', 'RefundPayments'], expiresAt: new Date(Date.now() + 900000).toISOString() }, admin ? 200 : 403);
    if (path === '/auth/refresh') return reply({ token: 'fixture-only-token', user: { id: 'fixture', name: 'Fixture customer', email: 'fixture@example.test', roles: [admin ? 'Admin' : 'Customer'] } });
    if (path === '/content/settings' || path === '/catalog/categories' || path === '/catalog/collections') return reply([]);
    if (path === '/cart') { state.cartReads++; return reply({ totalItems: state.status === 'Paid' ? 0 : mixed ? 6 : 2, items: state.status === 'Paid' ? [] : items.map(i => ({ ...i, id: i.cartItemId, productId: i.cartItemId, productName: i.name })), total: subtotal }); }
    if (path === '/wishlist') return reply({ items: [], totalItems: 0 });
    if (path === '/addresses') return reply([{ id: 1, label: 'Home', recipientName: 'Fixture customer', city: 'Colombo', addressLine1: 'Test street', country: 'Sri Lanka', phone: '+94770000000', isDefault: true }, { id: 2, label: 'Outside zone', recipientName: 'Fixture customer', city: 'Test city', country: 'Test country' }]);
    if (path === '/checkout/quote') {
      const data = request.postDataJSON(); state.quotes.push(data);
      const invalid = Boolean(data.couponCode && data.couponCode !== 'TEST10');
      const discount = data.couponCode === 'TEST10' ? 10 : 0;
      return reply({ items, subtotal, discount, shipping: data.shippingMethodId ? 25 : 0, tax: 0, paymentFee: 0, total: subtotal - discount + (data.shippingMethodId ? 25 : 0), currency: 'CHF', availableShippingMethods: data.addressId === 2 ? [] : [method], paymentAvailable, paymentProvider: paymentAvailable ? 'Zahls' : null, paymentMode: paymentAvailable ? 'TEST' : null, canPay: paymentAvailable && data.addressId === 1 && data.shippingMethodId === 1 && !invalid, couponResult: data.couponCode ? { code: data.couponCode, applied: !invalid, message: invalid ? 'Coupon is unavailable.' : 'Test coupon applied.', discount } : null, quoteVersion: 'test-quote-version', expiresAt: new Date(Date.now() + 300000).toISOString() });
    }
    if (path === '/checkout/session') { state.sessions.push(request.postDataJSON()); await new Promise(resolve => setTimeout(resolve, 250)); return reply({ orderId: 1, paymentId: 1, status: 'Pending', redirectUrl: 'https://pay.zahls.ch/TEST-ONLY-1' }); }
    if (path === '/checkout/orders/1/retry') { state.retries++; state.status = 'Pending'; return reply({ orderId: 1, paymentId: 1, status: 'Pending', redirectUrl: 'https://pay.zahls.ch/TEST-ONLY-1' }); }
    if (path === '/payments/1' || path === '/admin/payments/1') return reply(payment());
    if (path === '/orders/1' || path === '/admin/orders/1') return reply(order());
    if (path === '/orders') return reply([order()]);
    if (path === '/admin/payments/1/refunds') { state.refunds.push({ ...request.postDataJSON(), id: state.refunds.length + 1, currency: 'CHF', status: 'Pending' }); return reply(state.refunds.at(-1)); }
    if (path === '/admin/shipping') return reply({ zones: [], methods: [] });
    return reply({ title: 'Unhandled test route: ' + path }, 404);
  });
  await page.route('https://pay.zahls.ch/**', route => route.fulfill({ contentType: 'text/html', body: '<!doctype html><script>location.replace(' + JSON.stringify(frontend + 'checkout/payment/success?paymentId=1&status=Paid') + ')</script>' }));
  return state;
}
