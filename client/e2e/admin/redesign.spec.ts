import { test, expect, acceptanceApi, type Page } from '../fixtures';
import AxeBuilder from '@axe-core/playwright';
const destinations = ['dashboard', 'products', 'categories', 'collections', 'media', 'content', 'promotions', 'custom-builder', 'orders', 'custom-designs', 'quote-requests', 'inventory', 'shipping', 'customers', 'payments', 'reviews', 'privacy-requests', 'reports', 'audit', 'staff', 'settings'];
const keyPages = ['dashboard', 'payments', 'promotions', 'reports', 'audit', 'staff', 'privacy-requests'];
async function openOperations(page: Page) {
  const response = await page.request.post(acceptanceApi + '/__fixture/operations'); expect(response.ok()).toBe(true); const fixture = await response.json();
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: fixture.superAdminEmail, password: fixture.basic.password, authenticatorCode: code } })).ok()).toBe(true);
  return fixture;
}
for (const width of [1920, 1440, 1024, 768, 390]) {
  test(`admin pages preserve navigation, fit ${width}px and pass key-page accessibility`, async ({ page }) => {
    test.setTimeout(240000); await page.setViewportSize({ width, height: 1080 }); await page.emulateMedia({ reducedMotion: 'reduce' });
    const fixture = await openOperations(page); const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
    page.on('console', message => { if (message.type() === 'error' || message.type() === 'warning') errors.push(message.text()); });
    for (const route of [...destinations, `orders/${fixture.basic.orderId}`, `customers/${fixture.customerId}`, `custom-designs/${fixture.designId}`]) {
      await page.goto('./admin/' + route); await expect(page.locator('.admin-content h1')).toBeVisible();
      await expect(page.getByText('Loading details…', { exact: true })).toHaveCount(0);
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), { message: route + ' at ' + width }).toBe(true);
      await page.keyboard.press('Tab'); await expect(page.getByRole('link', { name: 'Skip to main content', exact: true })).toBeFocused();
      await page.keyboard.press('Enter'); await expect(page.locator('main')).toBeFocused();
      if (keyPages.includes(route) || width === 1440 || width === 390) {
        const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
        expect(result.violations.map(value => ({ id: value.id, nodes: value.nodes.map(node => ({ target: node.target, summary: node.failureSummary })) })), route + ' accessibility').toEqual([]);
        if (keyPages.includes(route)) await page.screenshot({ path: `../.runtime/admin-after-${route}-${width}.png`, fullPage: true });
      }
    }
    await page.goto('./admin/dashboard');
    if (width < 768) {
      await page.getByRole('button', { name: 'Toggle admin navigation', exact: true }).click();
      await expect(page.getByRole('dialog', { name: 'Admin navigation', exact: true })).toBeVisible();
      await page.keyboard.press('Escape'); await expect(page.getByRole('button', { name: 'Toggle admin navigation', exact: true })).toBeFocused();
    } else {
      await expect(page.locator('#admin-navigation')).toHaveCSS('width', width >= 1280 ? '264px' : '72px');
      await expect(page.locator('#admin-navigation nav a').first()).toHaveAttribute('aria-label', 'Dashboard');
      await expect(page.getByRole('link', { name: 'Dashboard', exact: true })).toHaveAttribute('aria-current', 'page');
    }
    await page.getByRole('button', { name: /^Open user menu/ }).click();
    await expect(page.getByRole('menuitem', { name: 'Sign out / reauthenticate', exact: true })).toBeFocused();
    await page.keyboard.press('Escape'); await expect(page.getByRole('button', { name: /^Open user menu/ })).toBeFocused();
    expect(errors).toEqual([]);
  });
}
test('privacy retry and empty payment states are clear without discarding filter choices', async ({ page }) => {
  await openOperations(page);
  await page.route('**/api/v1/admin/customer-requests/privacy?*', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Service unavailable' }) }));
  await page.goto('./admin/privacy-requests'); await expect(page.getByRole('heading', { name: "We couldn't load privacy requests", exact: true })).toBeVisible();
  await page.unroute('**/api/v1/admin/customer-requests/privacy?*'); await page.getByRole('button', { name: 'Try again', exact: true }).click(); await expect(page.getByRole('table', { name: 'Privacy request queue' })).toBeVisible();
  await page.route('**/api/v1/admin/payments?*', route => route.fulfill({ contentType: 'application/json', body: JSON.stringify({ items: [], page: 1, pageSize: 30, totalCount: 0 }) }));
  await page.goto('./admin/payments'); await expect(page.getByText('No payment attempts match these filters', { exact: true })).toBeVisible();
  await page.getByRole('combobox', { name: 'Payment state', exact: true }).selectOption('Failed');
  await expect(page.getByRole('combobox', { name: 'Payment state', exact: true })).toHaveValue('Failed');
  await page.getByRole('button', { name: 'Reset filters', exact: true }).click(); await expect(page.getByRole('combobox', { name: 'Payment state', exact: true })).toHaveValue('');
});
test('staff change keeps review, focus restore and original request payload', async ({ page }) => {
  const fixture = await openOperations(page); await page.goto('./admin/staff');
  await page.getByLabel('Verified account email', { exact: true }).fill(fixture.supportEmail);
  await page.getByRole('checkbox', { name: /^CustomerSupport/ }).check();
  const assignment = page.locator('form').filter({ has: page.getByRole('heading', { name: 'Replace staff role assignment', exact: true }) });
  await assignment.getByLabel('Reason', { exact: true }).fill('Synthetic UI verification of staff review');
  const review = page.getByRole('button', { name: 'Review staff change', exact: true }); await review.click();
  await expect(page.getByRole('dialog', { name: 'Confirm staff permissions', exact: true })).toContainText('Before:');
  await expect(page.getByRole('dialog')).toContainText('After:'); await page.keyboard.press('Escape'); await expect(review).toBeFocused();
  await review.click(); const sent = page.waitForRequest(request => request.method() === 'PUT' && request.url().includes('/admin/staff'));
  await page.getByRole('dialog').getByRole('button', { name: 'Confirm', exact: true }).click();
  const request = await sent; expect(request.postDataJSON()).toMatchObject({ email: fixture.supportEmail, roles: ['CustomerSupport'], reason: 'Synthetic UI verification of staff review' });
});
