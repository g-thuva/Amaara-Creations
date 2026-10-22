import { test, expect, type Page } from '@playwright/test';
import AxeBuilder from '@axe-core/playwright';
const lab = 'http://127.0.0.1:5208';
test.beforeEach(async ({ page }) => {
  page.on('pageerror', error => console.error('Browser error:', error.message));
});
async function snapshot(page: Page) { const response = await page.request.get(lab + '/simulator/api/inspect'); expect(response.ok()).toBe(true); return response.json(); }
async function checkout(page: Page) {
  await page.goto(lab + '/simulator');
  await expect(page.getByRole('heading', { name: 'Amaara payment lab', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Open shop as test customer', exact: true }).click();
  await expect(page).toHaveURL(/\/products$/);
  await expect(page.getByRole('complementary', { name: 'Simulated payments' })).toBeVisible();
  await page.goto('./products/1');
  await page.getByRole('button', { name: 'Add to cart', exact: true }).click();
  await expect(page.getByText('Added to your cart.', { exact: true })).toBeVisible();
  await page.goto('./checkout');
  const shipping = page.getByRole('combobox', { name: 'Shipping method', exact: true });
  const option = shipping.locator('option').filter({ hasText: 'SIMULATED delivery' }); await option.waitFor({ state: 'attached' });
  await shipping.selectOption((await option.getAttribute('value'))!);
  await page.getByRole('button', { name: 'Pay securely', exact: true }).click();
  await expect(page).toHaveURL(/5208\/simulator\/pay\/[0-9]+$/);
  await expect(page.locator('#summary')).toContainText('Pending');
  return (await snapshot(page)).payments[0];
}

test('synthetic administrator opens the real dashboard without entering credentials', async ({ page }) => {
  await page.goto(lab + '/simulator');
  await page.getByRole('button', { name: 'Open test admin', exact: true }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.getByRole('complementary', { name: 'Simulated payments' })).toBeVisible();
});

test('SQL-backed simulated success, event replay, partial/pending refunds and storefront verification use real APIs', async ({ page }) => {
  const payment = await checkout(page);
  expect(payment.provider).toBe('LocalSimulator'); expect(payment.providerMode).toBe('SIMULATED'); expect(payment.amount).toBe(126);
  await expect(page.locator('input[autocomplete^="cc-"],input[type="password"]')).toHaveCount(0);
  await page.getByRole('button', { name: 'Simulate success', exact: true }).click(); await expect(page.locator('#summary')).toContainText('Paid');
  await page.getByRole('button', { name: 'Replay last event', exact: true }).click();
  let saved = await snapshot(page); expect(saved.events.filter((event: { paymentId: number }) => event.paymentId === payment.id)).toHaveLength(1);
  expect(saved.inventory.find((value: { id: number }) => value.id === 1).stock).toBe(99);
  await page.getByLabel('Refund amount', { exact: true }).fill('20');
  await page.getByRole('button', { name: 'Request simulated refund', exact: true }).click(); await expect(page.locator('#notice')).toContainText('Completed');
  await page.getByRole('button', { name: 'Replay last refund request', exact: true }).click(); saved = await snapshot(page);
  expect(saved.refunds.filter((value: { paymentId: number }) => value.paymentId === payment.id)).toHaveLength(1);
  expect(saved.payments.find((value: { id: number }) => value.id === payment.id).refundedAmount).toBe(20);
  await page.getByLabel('Refund amount', { exact: true }).fill('10'); await page.getByRole('combobox', { name: 'Simulated refund outcome', exact: true }).selectOption('pending');
  await page.getByRole('button', { name: 'Request simulated refund', exact: true }).click(); await expect(page.locator('#notice')).toContainText('Pending');
  await page.getByRole('button', { name: "Complete selected payment's pending refund", exact: true }).click(); await expect(page.locator('#notice')).toContainText('completed through');
  saved = await snapshot(page); expect(saved.payments.find((value: { id: number }) => value.id === payment.id).refundedAmount).toBe(30);
  await page.getByLabel('Refund amount', { exact: true }).fill('1000'); await page.getByRole('combobox', { name: 'Simulated refund outcome', exact: true }).selectOption('success');
  await page.getByRole('button', { name: 'Request simulated refund', exact: true }).click(); await expect(page.getByRole('alert')).toContainText(/exceeds/);
  expect((await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze()).violations).toEqual([]);
  for (const width of [360, 768, 1440]) { await page.setViewportSize({ width, height: 900 }); expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), `${width}px lab overflow`).toBe(true); }
  await page.getByRole('link', { name: 'Return to storefront payment status', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Payment confirmed', exact: true })).toBeVisible();
  await page.goto('./orders'); await expect(page.getByRole('heading', { name: 'Your orders', exact: true })).toBeVisible();
});

test('decline and cancellation preserve cart and retry the same order; reconciliation completes pending payment', async ({ page }) => {
  const first = await checkout(page);
  await page.getByRole('button', { name: 'Simulate decline', exact: true }).click(); await expect(page.locator('#summary')).toContainText('Failed');
  await page.getByRole('link', { name: 'Return to storefront payment status', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Payment failed', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Retry secure payment', exact: true }).click(); await expect(page).toHaveURL(/5208\/simulator\/pay\/[0-9]+$/);
  await page.getByRole('button', { name: 'Simulate cancellation', exact: true }).click(); await expect(page.locator('#summary')).toContainText('Cancelled');
  await page.getByRole('link', { name: 'Return to storefront payment status', exact: true }).click(); await expect(page.getByRole('heading', { name: 'Payment cancelled', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Retry secure payment', exact: true }).click(); await expect(page).toHaveURL(/5208\/simulator\/pay\/[0-9]+$/);
  await page.getByRole('button', { name: 'Keep payment pending', exact: true }).click(); await expect(page.locator('#summary')).toContainText('Pending');
  await page.getByRole('combobox', { name: 'Event delivery', exact: true }).selectOption('reconciliation'); await page.getByRole('button', { name: 'Simulate success', exact: true }).click();
  await expect(page.locator('#notice')).toContainText('no webhook'); await expect(page.locator('#summary')).toContainText('Pending');
  await page.getByRole('button', { name: 'Reconcile selected payment', exact: true }).click(); await expect(page.locator('#summary')).toContainText('Paid');
  const saved = await snapshot(page); const attempts = saved.payments.filter((value: { orderId: number }) => value.orderId === first.orderId);
  expect(attempts).toHaveLength(3); expect(new Set(attempts.map((value: { orderId: number }) => value.orderId)).size).toBe(1);
  expect(attempts.map((value: { status: string }) => value.status).sort()).toEqual(['Cancelled', 'Failed', 'Paid']);
});

test('the local host rejects untrusted origins, missing run cookies, rebinding and credential collection', async ({ page, playwright }) => {
  const anonymous = await playwright.request.newContext();
  try { expect((await anonymous.get(lab + '/simulator/api/inspect')).status()).toBe(403); } finally { await anonymous.dispose(); }
  await page.goto(lab + '/simulator');
  expect((await page.request.post(lab + '/simulator/api/events', { headers: { Origin: 'https://evil.example.test' }, data: {} })).status()).toBe(403);
  expect((await page.request.get(lab + '/simulator/health', { headers: { Host: 'evil.example.test:5208' } })).status()).toBe(404);
  expect((await page.request.post(lab + '/api/v1/auth/sign-in', { headers: { Origin: 'http://127.0.0.1:5188' }, data: { email: 'do-not-enter-real-data@example.test', password: 'synthetic-test-only' } })).status()).toBe(403);
});
