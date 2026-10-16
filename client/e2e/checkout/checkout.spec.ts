import { test, expect } from '@playwright/test';
import { checkoutFixture } from './checkoutFixture';

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
