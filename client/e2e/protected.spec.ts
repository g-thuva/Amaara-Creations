import { test, expect } from '@playwright/test';
const email = process.env.STOREFRONT_TEST_EMAIL;
const password = process.env.STOREFRONT_TEST_PASSWORD;
test('protected customer pages and admin remain responsive', async ({ page }) => {
  test.skip(!email || !password, 'Set local test credentials to enable protected browser checks.');
  test.setTimeout(180000);
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await page.goto('./#/login');
  await page.getByLabel('Email Address', { exact: true }).fill(email!);
  await page.getByLabel('Password', { exact: true }).fill(password!);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page).toHaveURL(/#\/profile$/);
  for (const width of [375, 430, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    for (const route of ['profile', 'addresses', 'orders', 'wishlist', 'cart', 'account/security']) {
      await page.goto(`./#/${route}`);
      await expect(page.locator('main h1')).toBeVisible();
      await expect(page.locator('main [aria-label="Loading content"]')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), `${route} at ${width}`).toBe(true);
    }
  }
  await page.goto('./#/admin/dashboard');
  await expect(page).toHaveURL(/#\/admin\/dashboard$/);
  await expect(page.locator('main')).toBeVisible();
  expect(errors).toEqual([]);
});
