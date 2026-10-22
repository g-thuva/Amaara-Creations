import { fillAuthenticator } from '../staffAuth';
import { test, expect } from '../fixtures';
import AxeBuilder from '@axe-core/playwright';
import { mkdir, writeFile } from 'node:fs/promises';

test('built public routes reload cleanly and expose canonical product metadata', async ({ page, fixture, request }) => {
  const errors: string[] = []; page.on('pageerror', e => errors.push(e.message));
  await page.goto(`./products/${fixture.products[0]}`); await expect(page.getByRole('heading', { level: 1 })).toHaveText('Storefront QA print 01');
  await expect(page).toHaveTitle('Storefront QA print 01 | Amaara Creations'); await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', `https://store.example.test/Amaara-Creations/products/${fixture.products[0]}`);
  const schema = await page.locator('#product-schema').textContent(); expect(JSON.parse(schema!).offers.priceCurrency).toBe('LKR');
  await page.reload(); await expect(page.getByRole('button', { name: 'Add to cart', exact: true })).toBeVisible();
  const sitemap = await request.get('http://127.0.0.1:5198/api/v1/seo/sitemap.xml'); expect(sitemap.ok()).toBe(true); const xml = await sitemap.text(); expect(xml).toContain('/products/' + fixture.products[0]); expect(xml).not.toMatch(/\/admin|\/account|\/checkout|\/api\/v1\/custom-designs/);
  await page.goto('./login'); await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', 'noindex,nofollow'); await expect(page.locator('link[rel="canonical"]')).toHaveCount(0); expect(errors).toEqual([]);
  await page.goto('./products/2147483647'); await expect(page.getByRole('heading', { level: 1 })).toHaveText('Product not found');
  await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', 'noindex,nofollow'); await expect(page.locator('link[rel="canonical"]')).toHaveCount(0); await expect(page.locator('#product-schema')).toHaveCount(0);
  await page.route('**/api/v1/content/pages/published-seo', route => route.fulfill({ json: { id: 1, title: 'Published information', slug: 'published-seo', summary: 'Useful public information.', isPublished: true, sections: [] } }));
  await page.goto('./pages/published-seo'); await expect(page).toHaveTitle('Published information | Amaara Creations');
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', 'https://store.example.test/Amaara-Creations/pages/published-seo');
  await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', 'index,follow');
  await page.goto('./pages/missing-seo'); await expect(page.getByRole('heading', { level: 1 })).toHaveText('Page not found');
  await expect(page.locator('meta[name="robots"]')).toHaveAttribute('content', 'noindex,nofollow'); await expect(page.locator('link[rel="canonical"]')).toHaveCount(0);
});

test('built storefront meets local lab LCP, CLS and entry-bundle budgets', async ({ page }, testInfo) => {
  await page.addInitScript(() => {
    const metrics = { lcp: 0, cls: 0, shifts: [] as unknown[] }; (window as unknown as { releaseMetrics: typeof metrics }).releaseMetrics = metrics;
    new PerformanceObserver(list => { for (const entry of list.getEntries()) metrics.lcp = entry.startTime; }).observe({ type: 'largest-contentful-paint', buffered: true });
    new PerformanceObserver(list => { for (const entry of list.getEntries()) { const shift = entry as PerformanceEntry & { hadRecentInput: boolean; value: number; sources?: { node?: Element; previousRect: DOMRectReadOnly; currentRect: DOMRectReadOnly }[] }; if (!shift.hadRecentInput) { metrics.cls += shift.value; metrics.shifts.push({ value: shift.value, sources: shift.sources?.map(s => ({ element: s.node?.tagName + '.' + s.node?.className, before: s.previousRect, after: s.currentRect })) }); } } }).observe({ type: 'layout-shift', buffered: true });
  });
  await page.goto('./'); await expect(page.getByRole('heading', { level: 1 })).toBeVisible(); await page.evaluate(() => document.fonts.ready);
  await expect(page.locator('.s-category-card').first()).toBeVisible();
  await expect.poll(() => page.evaluate(() => (window as unknown as { releaseMetrics: { lcp: number } }).releaseMetrics.lcp)).toBeGreaterThan(0);
  const metrics = await page.evaluate(() => ({ ...(window as unknown as { releaseMetrics: { lcp: number; cls: number } }).releaseMetrics, navigationMs: performance.now(), assets: performance.getEntriesByType('resource').filter(r => /\/assets\/.*\.js/.test(r.name)).map(r => ({ name: new URL(r.name).pathname.split('/').at(-1), bytes: (r as PerformanceResourceTiming).encodedBodySize })) }));
  await testInfo.attach('local-production-build-performance', { body: JSON.stringify({ ...metrics, productionFieldGuarantee: false }), contentType: 'application/json' });
  await mkdir('../.runtime', { recursive: true }); await writeFile('../.runtime/release-performance.json', JSON.stringify({ ...metrics, productionFieldGuarantee: false }));
  await page.screenshot({ path: '../.runtime/release-home-desktop.png' });
  expect(metrics.lcp, 'Local desktop lab LCP, not a production field guarantee').toBeLessThan(4000); expect(metrics.cls).toBeLessThan(.1);
});

test('built admin and customer forms stay accessible across mobile and desktop', async ({ page, fixture }) => {
  await page.goto('http://127.0.0.1:5179/Amaara-Creations/login'); await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail); await page.getByLabel('Password', { exact: true }).fill(fixture.password); await fillAuthenticator(page); await page.getByRole('button', { name: /^(Sign In|Verify and sign in)$/ }).click(); await expect(page).not.toHaveURL(/\/login$/);
  for (const width of [375, 1440]) {
    await page.setViewportSize({ width, height: 900 }); await page.goto('http://127.0.0.1:5179/Amaara-Creations/admin/inventory'); await expect(page.getByRole('heading', { name: 'Inventory ledger', exact: true })).toBeVisible(); await expect(page.getByRole('table', { name: 'Stock balances' })).toBeVisible();
    if (width === 375) {
      const toggle = page.getByRole('button', { name: 'Toggle admin navigation' });
      expect((await toggle.locator('svg').boundingBox())!.width).toBeGreaterThanOrEqual(20);
      await toggle.click(); await expect(page.getByRole('navigation', { name: 'Administration' }).getByRole('link', { name: 'Payments', exact: true })).toBeVisible();
      await page.getByRole('navigation', { name: 'Administration' }).getByRole('link', { name: 'Inventory', exact: true }).focus(); await page.keyboard.press('Escape'); await expect(toggle).toBeFocused(); await expect(toggle).toHaveAttribute('aria-expanded', 'false');
    }
    const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze(); expect(result.violations).toEqual([]); expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.screenshot({ path: `../.runtime/release-inventory-${width}.png` });
  }
});
