import { test, expect } from '../fixtures';
import { checkAccessibility as check } from '../accessibility';
test('admin operations pass automated WCAG rules and keyboard navigation', async ({ page, fixture }) => {
  test.setTimeout(180000); await page.emulateMedia({ reducedMotion: 'reduce' }); await page.goto('./login'); await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail); await page.getByLabel('Password', { exact: true }).fill(fixture.password); await page.getByRole('button', { name: 'Sign In', exact: true }).click(); await expect(page).toHaveURL(/\/profile$/);
  for (const route of ['admin/orders', 'admin/customers', 'admin/custom-designs', 'admin/inventory', 'admin/payments', 'admin/promotions', 'admin/reviews', 'admin/reports', 'admin/audit']) await check(page, route);
  await page.goto('./admin/inventory'); await page.keyboard.press('Tab'); await expect(page.getByRole('link', { name: 'Skip to main content', exact: true })).toBeFocused(); await page.keyboard.press('Enter'); await expect(page.locator('main')).toBeFocused();
});
