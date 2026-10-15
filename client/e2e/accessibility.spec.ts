import AxeBuilder from '@axe-core/playwright';
import { test, expect } from './fixtures';

const tags = ['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa'];
async function check(page: import('@playwright/test').Page, route: string) {
  await page.goto('./' + route); await expect(page.locator('main h1')).toBeVisible();
  await expect(page.locator('[aria-label="Loading content"]')).toHaveCount(0);
  await page.evaluate(() => document.fonts.ready);
  const result = await new AxeBuilder({ page }).withTags(tags).analyze();
  expect(result.violations.map(v => ({ id: v.id, impact: v.impact, nodes: v.nodes.map(n => ({ target: n.target, summary: n.failureSummary })) })), route).toEqual([]);
}
test('public catalog, builder and account entry forms pass automated WCAG rules', async ({ page, fixture }) => {
  test.setTimeout(180000); await page.emulateMedia({ reducedMotion: 'reduce' });
  for (const route of ['', 'products', `products/${fixture.products[0]}`, 'custom', 'login', 'register', 'forgot-password']) await check(page, route);
});
test('admin operations pass automated WCAG rules and keyboard navigation', async ({ page, fixture }) => {
  test.setTimeout(180000); await page.emulateMedia({ reducedMotion: 'reduce' }); await page.goto('./login'); await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail); await page.getByLabel('Password', { exact: true }).fill(fixture.password); await page.getByRole('button', { name: 'Sign In', exact: true }).click(); await expect(page).toHaveURL(/\/profile$/);
  for (const route of ['admin/orders', 'admin/customers', 'admin/custom-designs', 'admin/inventory', 'admin/payments', 'admin/promotions', 'admin/reviews', 'admin/reports', 'admin/audit']) await check(page, route);
  await page.goto('./admin/inventory'); await page.keyboard.press('Tab'); await expect(page.getByRole('link', { name: 'Skip to main content', exact: true })).toBeFocused(); await page.keyboard.press('Enter'); await expect(page.locator('main')).toBeFocused();
});
