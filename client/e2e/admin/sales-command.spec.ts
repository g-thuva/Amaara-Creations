import { test, expect, acceptanceApi, type Page } from '../fixtures';
import AxeBuilder from '@axe-core/playwright';
const widths = [1920, 1440, 1280, 1024, 768, 390];
async function openDashboard(page: Page) {
  const response = await page.request.post(acceptanceApi + '/__fixture/operations');
  expect(response.ok()).toBe(true); const fixture = await response.json();
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: fixture.superAdminEmail, password: fixture.basic.password, authenticatorCode: code } })).ok()).toBe(true);
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('./admin/dashboard');
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
}
async function checkLayout(page: Page, state: string) {
  for (const width of widths) {
    const height = width === 1920 ? 1080 : width >= 1280 ? 900 : 1000;
    await page.setViewportSize({ width, height });
    await page.screenshot({ path: `../.runtime/sales-command/${state}-${width}.png`, fullPage: true });
    await expect.poll(() => page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    if (width >= 1280) {
      const layout = await page.evaluate(() => ({ viewport: innerHeight, height: document.documentElement.scrollHeight, elements: [...document.querySelectorAll('.sales-command, .sales-stat, .sales-panel, .sales-chart-body, .sales-chart-transition, .sales-bars, .sales-chart-footer')].map(element => ({ class: element.getAttribute('class'), height: element.getBoundingClientRect().height, bottom: element.getBoundingClientRect().bottom, scrollHeight: element.scrollHeight })) }));
      await expect.poll(() => page.evaluate(() => document.documentElement.scrollHeight <= innerHeight), { message: JSON.stringify(layout) }).toBe(true);
      for (const selector of ['.sales-command-header', '.sales-kpis', '.sales-overview', '.sales-breakdown', '.sales-recent', '.sales-products']) {
        const box = await page.locator(selector).boundingBox(); expect(box!.y + box!.height).toBeLessThanOrEqual(height);
      }
      const overview = page.locator('.sales-overview');
      const footer = page.locator('.sales-chart-footer');
      if (await footer.count()) { const panelBox = await overview.boundingBox(), box = await footer.boundingBox(); expect(box!.y + box!.height).toBeLessThanOrEqual(panelBox!.y + panelBox!.height); }
      const emptyState = page.locator('.sales-chart-overlay .admin-empty-state');
      if (await emptyState.count()) { const chartBox = await page.locator('.sales-chart-body').boundingBox(), box = await emptyState.boundingBox(); expect(box!.y).toBeGreaterThanOrEqual(chartBox!.y); expect(box!.y + box!.height).toBeLessThanOrEqual(chartBox!.y + chartBox!.height); }
    }
    const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze();
    expect(result.violations.map(value => ({ id: value.id, nodes: value.nodes.map(node => ({ target: node.target, summary: node.failureSummary })) }))).toEqual([]);
  }
}
test('real API summaries remain honest and fit desktop/tablet/mobile', async ({ page }) => {
  test.setTimeout(180000); const errors: string[] = []; page.on('pageerror', error => errors.push(error.message));
  await openDashboard(page);
  await expect(page.getByText('Sales history is not available', { exact: true })).toBeVisible();
  await expect(page.getByText('All time · period comparison unavailable')).toBeVisible();
  for (const option of ['7D', '3M', '12M', '30D']) { await page.getByRole('button', { name: option, exact: true }).click(); await expect(page.getByRole('button', { name: option, exact: true })).toHaveAttribute('aria-pressed', 'true'); }
  await checkLayout(page, 'api-populated-unavailable-history');
  await page.setViewportSize({ width: 1280, height: 800 });
  expect(await page.evaluate(() => document.documentElement.scrollHeight <= innerHeight)).toBe(true);
  expect(errors).toEqual([]);
});
test('empty data has clearly labelled placeholders and a zero table', async ({ page }) => {
  test.setTimeout(180000);
  await page.route('**/api/v1/admin/dashboard**', route => {
    const path = new URL(route.request().url()).pathname;
    const body = path.endsWith('recent-orders') ? [] : path.endsWith('products') ? { topSellingProducts: [], lowStockProducts: 0 } : { totalRevenue: 0, totalOrders: 0, totalCustomers: 0, averageRating: 0, monthlyRevenue: [], pendingOrders: 0 };
    return route.fulfill({ contentType: 'application/json', body: JSON.stringify(body) });
  });
  await openDashboard(page);
  await expect(page.getByText('No sales in this period yet', { exact: true })).toBeVisible();
  await expect(page.locator('.sales-ghost-bar')).toHaveCount(12);
  await page.getByRole('button', { name: 'View as table' }).click();
  await expect(page.getByRole('table', { name: 'Sales overview data' })).toBeVisible();
  await expect(page.getByRole('table', { name: 'Sales overview data' }).locator('tbody tr')).toHaveCount(30);
  await page.getByRole('button', { name: 'Hide table' }).click();
  await checkLayout(page, 'empty');
});
test('revenue failure is independent and retries without resetting period', async ({ page }) => {
  test.setTimeout(180000);
  await page.route('**/api/v1/admin/dashboard/revenue', route => route.fulfill({ status: 503, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Unavailable' }) }));
  await openDashboard(page);
  await expect(page.getByRole('heading', { name: 'Sales overview could not be loaded' })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Recent orders', exact: true })).toBeVisible();
  await checkLayout(page, 'error');
  await page.getByRole('button', { name: '7D', exact: true }).click();
  await page.unroute('**/api/v1/admin/dashboard/revenue');
  await page.locator('.sales-overview').getByRole('button', { name: 'Try again', exact: true }).click();
  await expect(page.getByText('Sales history is not available', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: '7D', exact: true })).toHaveAttribute('aria-pressed', 'true');
});
test('loading revenue leaves independent cards available', async ({ page }) => {
  test.setTimeout(180000);
  let release!: () => void;
  const gate = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/v1/admin/dashboard/revenue', async route => { await gate; await route.continue().catch(() => {}); });
  try {
    await openDashboard(page);
    await expect(page.locator('.sales-skeleton-bar')).toHaveCount(12);
    await expect(page.getByRole('heading', { name: 'Recent orders', exact: true })).toBeVisible();
    await checkLayout(page, 'loading');
  } finally { release(); }
});
test('populated chart supports keyboard, touch, table, legend and reduced motion', async ({ page }) => {
  test.setTimeout(120000);
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.goto('./e2e/fixtures/sales-charts.html');
  await expect(page.getByText('Synthetic test fixtures · never displayed in the production dashboard')).toBeVisible();
  const bars = page.locator('.sales-bar'); await expect(bars).toHaveCount(30);
  await bars.first().focus(); await expect(page.getByRole('tooltip')).toContainText('01/10/2026');
  await page.keyboard.press('ArrowRight'); await expect(bars.nth(1)).toBeFocused(); await expect(page.getByRole('tooltip')).toContainText('02/10/2026');
  await page.keyboard.press('Escape'); await expect(page.getByRole('tooltip')).toHaveCount(0);
  await bars.nth(5).click(); await expect(page.getByRole('tooltip')).toContainText('06/10/2026');
  await page.getByLabel('Previous period', { exact: true }).check(); await expect(page.locator('.sales-previous')).toHaveCount(1);
  await page.getByRole('button', { name: 'View as table', exact: true }).click(); await expect(page.getByRole('table', { name: 'Sales overview data' }).locator('tbody tr')).toHaveCount(30);
  await page.getByRole('button', { name: 'Hide table', exact: true }).click();
  await page.locator('.sales-legend-row').first().focus(); await expect(page.locator('.sales-donut-segment').first()).toHaveClass(/is-active/);
  await expect(bars.first()).toHaveCSS('animation-name', 'none');
  for (const width of [1440, 1024, 390]) {
    await page.setViewportSize({ width, height: 1000 });
    const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21aa', 'wcag22aa']).analyze(); expect(result.violations).toEqual([]);
    await page.screenshot({ path: `../.runtime/sales-command/chart-fixture-populated-${width}.png`, fullPage: true });
  }
});
