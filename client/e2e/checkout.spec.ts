import { test, expect, type Page } from '@playwright/test';

// These fixtures test UI behaviour only. They never call or claim a real Zahls payment.
async function checkoutFixture(page: Page, { mixed = false, admin = false, paymentAvailable = true } = {}) {
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

for (const mixed of [false, true]) test('checkout sends identifiers once and waits for verified payment: ' + (mixed ? 'mixed cart' : 'standard'), async ({ page }) => {
  const state = await checkoutFixture(page, { mixed }); const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await page.goto('./checkout');
  const pay = page.getByRole('button', { name: 'Pay securely', exact: true });
  await expect(pay).toBeDisabled(); await page.getByRole('combobox', { name: 'Shipping method', exact: true }).selectOption('1');
  await expect(pay).toBeEnabled();
  await expect(page.getByText('Test payment — no real money will be charged.', { exact: true })).toBeVisible();
  await expect(page.getByText('Currency: CHF', { exact: true })).toBeVisible();
  await expect(page.getByRole('definition').last()).toContainText(mixed ? '273.00' : '225.00');
  if (mixed) await expect(page.getByText('Fixture custom sticker', { exact: true })).toBeVisible();
  await pay.evaluate(button => { (button as HTMLButtonElement).click(); (button as HTMLButtonElement).click(); });
  await expect(page).toHaveURL(/payment\/success/);
  await expect(page.getByRole('heading', { level: 1, name: 'Payment processing' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Payment confirmed' })).toHaveCount(0);
  expect(state.sessions).toHaveLength(1); const body = state.sessions[0];
  expect(body.addressId).toBe(1); expect(body.shippingMethodId).toBe(1); expect(body.quoteVersion).toBe('test-quote-version');
  expect(body.idempotencyKey).toMatch(/^[a-f0-9-]{36}$/);
  for (const forbidden of ['total', 'shippingPrice', 'discountAmount', 'status', 'cardNumber', 'cvv']) expect(body).not.toHaveProperty(forbidden);
  const reads = state.cartReads; state.status = 'Paid'; state.orderStatus = 'Confirmed';
  await page.getByRole('button', { name: 'Check again', exact: true }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Payment confirmed' })).toBeVisible(); await expect.poll(() => state.cartReads).toBeGreaterThan(reads);
  await page.getByRole('link', { name: 'View order', exact: true }).click(); await expect(page.getByText('Payment: Paid · Fulfilment: NotReady')).toBeVisible();
  expect(errors).toEqual([]);
});

test('quote refreshes after coupon and address changes and blocks invalid checkout', async ({ page }) => {
  const state = await checkoutFixture(page); await page.goto('./checkout');
  await page.getByRole('combobox', { name: 'Shipping method', exact: true }).selectOption('1');
  await page.getByLabel('Coupon code').fill('BAD'); await page.getByRole('button', { name: 'Apply coupon' }).click();
  await expect(page.getByRole('alert')).toContainText('Coupon is unavailable.');
  await expect(page.getByRole('button', { name: 'Pay securely', exact: true })).toBeDisabled();
  await page.getByLabel('Coupon code').fill('TEST10'); await page.getByRole('button', { name: 'Apply coupon' }).click();
  await expect(page.getByRole('definition').last()).toContainText('215.00');
  await page.getByRole('combobox', { name: 'Saved address', exact: true }).selectOption('2'); await expect(page.getByRole('alert')).toContainText('No delivery option');
  await expect(page.getByRole('combobox', { name: 'Shipping method', exact: true })).toHaveValue('');
  await expect(page.getByRole('button', { name: 'Pay securely', exact: true })).toBeDisabled();
  expect(state.sessions).toHaveLength(0); expect(state.quotes.at(-1)?.addressId).toBe(2);
});

test('unconfigured payment gateway is not offered by checkout', async ({ page }) => {
  const state = await checkoutFixture(page, { paymentAvailable: false }); await page.goto('./checkout');
  await page.getByRole('combobox', { name: 'Shipping method', exact: true }).selectOption('1');
  await expect(page.getByText('Secure online payment is not available for this checkout yet. Your cart has not been charged.', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Pay securely', exact: true })).toBeDisabled();
  expect(state.sessions).toHaveLength(0);
});

for (const status of ['Failed', 'Cancelled']) test('payment ' + status + ' preserves cart and retries the same order', async ({ page }) => {
  const state = await checkoutFixture(page); state.status = status;
  await page.goto('./checkout/payment/success?paymentId=1&status=Paid');
  await expect(page.getByRole('heading', { name: status === 'Failed' ? 'Payment failed' : 'Payment cancelled', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Retry secure payment' }).click();
  await expect(page.getByRole('heading', { name: 'Payment processing', exact: true })).toBeVisible();
  expect(state.retries).toBe(1); expect(state.sessions).toHaveLength(0);
});

test('checkout and return pages fit required widths and expose labelled keyboard controls', async ({ page }) => {
  await checkoutFixture(page, { mixed: true });
  for (const width of [375, 430, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 900 }); await page.goto('./checkout');
    await expect(page.getByRole('combobox', { name: 'Shipping method', exact: true })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), width + 'px checkout').toBe(true);
    await page.getByRole('combobox', { name: 'Saved address', exact: true }).focus(); await expect(page.getByRole('combobox', { name: 'Saved address', exact: true })).toBeFocused();
    await page.getByRole('combobox', { name: 'Shipping method', exact: true }).selectOption('1'); await expect(page.getByRole('button', { name: 'Pay securely', exact: true })).toBeEnabled();
    await page.goto('./checkout/payment/return?paymentId=1');
    await expect(page.getByRole('heading', { name: 'Payment processing', exact: true })).toBeVisible();
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), width + 'px return').toBe(true);
    await page.getByRole('button', { name: 'Check again', exact: true }).focus(); await page.keyboard.press('Enter');
    await expect(page.getByRole('heading', { name: 'Payment processing', exact: true })).toBeVisible();
  }
});

test('admin refund requires confirmation, preserves pending status and supports Escape focus restoration', async ({ page }) => {
  const state = await checkoutFixture(page, { admin: true }); state.status = 'Paid'; state.orderStatus = 'Confirmed';
  let releaseRefresh!: () => void;
  const refreshGate = new Promise<void>(resolve => { releaseRefresh = resolve; });
  let heldRefreshes = 0;
  await page.route(/\/api\/v1\/admin\/(orders|payments)\/1$/, async route => {
    if (state.refunds.length) { heldRefreshes++; await refreshGate; }
    await route.fallback();
  });
  await page.goto('./admin/orders/1'); await page.getByLabel('Refund amount').fill('50'); await page.getByLabel('Refund reason').fill('Fixture refund reason');
  const review = page.getByRole('button', { name: 'Review refund', exact: true }); await review.click();
  await expect(page.getByRole('dialog')).toBeVisible(); expect(state.refunds).toHaveLength(0);
  await page.keyboard.press('Escape'); await expect(page.getByRole('dialog')).not.toBeVisible(); await expect(review).toBeFocused();
  await review.click(); await page.getByRole('dialog').getByRole('button', { name: 'Confirm', exact: true }).click();
  try {
    await expect.poll(() => heldRefreshes).toBeGreaterThanOrEqual(2);
    await expect(page.getByText('Request status: Pending.', { exact: true })).toBeVisible();
  } finally { releaseRefresh(); }
  await expect(page.getByText('Request status: Pending.', { exact: true })).toBeVisible(); expect(state.refunds).toHaveLength(1);
  await expect(page.getByText(/50.00 — Pending: Fixture refund reason/)).toBeVisible();
  await page.goto('./admin/shipping'); await expect(page.getByRole('heading', { name: 'Shipping zones and methods', exact: true })).toBeVisible();
  for (const width of [375, 430, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), { message: width + 'px admin shipping' }).toBe(true);
  }
});

test('customer cannot open admin payment operations', async ({ page }) => {
  await checkoutFixture(page); await page.goto('./admin/orders/1'); await expect(page.getByRole('heading', { name: 'Access denied' })).toBeVisible();
});
