import { test, expect } from '../fixtures';
test('protected customer pages remain responsive and staff routes are absent', async ({ page, fixture }) => {
  test.setTimeout(180000);
  const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: /^(Sign In|Verify and sign in)$/ }).click();
  await expect(page).toHaveURL(/\/profile$/);
  for (const width of [320, 375, 430, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    for (const route of ['profile', 'addresses', 'orders', 'wishlist', 'cart', 'account/security']) {
      await page.goto(`./${route}`);
      await expect(page.locator('main h1')).toBeVisible();
      await expect(page.locator('main [aria-label="Loading content"]')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), `${route} at ${width}`).toBe(true);
    }
  }
  await page.goto('./admin/dashboard');
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await expect(page.getByRole('heading', { name: /not found|couldn.t find|page isn.t here/i })).toBeVisible();
  expect(errors).toEqual([]);
});
