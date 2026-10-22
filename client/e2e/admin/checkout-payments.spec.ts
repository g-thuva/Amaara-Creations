import { test, expect } from '@playwright/test';
import { checkoutFixture } from '../checkout/checkoutFixture';

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
  await checkoutFixture(page); await page.goto('./admin/orders/1'); await expect(page.getByRole('heading', { name: 'Welcome back' })).toBeVisible();
});
