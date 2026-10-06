import { test, expect } from '@playwright/test';
test('public routes, empty states, direct links and mobile keyboard navigation', async ({ page }) => {
  const failures: string[] = []; page.on('pageerror', error => failures.push(error.message));
  await page.goto('./');
  await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  for (const route of ['products', 'about', 'contact', 'custom', 'login', 'register', 'forgot-password', 'reset-password', 'verify-email', 'products/2147483647', 'not-a-route']) {
    await page.goto(`./#/${route}`);
    await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
  }
  await page.setViewportSize({ width: 375, height: 812 });
  await page.goto('./'); await page.getByRole('button', { name: 'Open navigation' }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  for (let i = 0; i < 12; i++) { await page.keyboard.press('Tab'); expect(await page.evaluate(() => !!document.activeElement?.closest('dialog'))).toBe(true); }
  await page.keyboard.press('Escape');
  await expect(page.getByRole('button', { name: 'Open navigation' })).toBeFocused();
  await page.goto('./#/products'); await page.getByRole('button', { name: /^Filters/ }).click();
  await expect(page.getByRole('dialog')).toBeVisible();
  await page.keyboard.press('Escape'); await expect(page.getByRole('button', { name: /^Filters/ })).toBeFocused();
  expect(failures).toEqual([]);
});
test('CMS hero carousel exposes named controls and honours reduced motion', async ({ page }) => {
  await page.route('**/api/v1/content/pages/home', async route => route.fulfill({
    status: 200,
    contentType: 'application/json',
    body: JSON.stringify({
      id: 1,
      title: 'Home',
      slug: 'home',
      summary: null,
      sections: [{
        id: 1,
        sectionKey: 'hero',
        contentType: 'json',
        sortOrder: 0,
        isActive: true,
        content: JSON.stringify({ slides: [
          { title: 'Wedding stickers', body: 'Personal details for the day.', ctaLabel: 'Shop', ctaUrl: '/products' },
          { title: 'Car decals', body: 'Made to feel like yours.', ctaLabel: 'Preview', ctaUrl: '/custom' },
        ] }),
      }],
    }),
  }));
  await page.goto('./');
  await expect(page.getByRole('region', { name: 'Featured' })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Pause carousel' })).toBeVisible();
  await page.getByRole('button', { name: 'Next slide' }).click();
  await expect(page.getByRole('heading', { name: 'Car decals' })).toBeVisible();
  await page.getByRole('button', { name: 'Pause carousel' }).click();
  await expect(page.getByRole('button', { name: 'Play carousel' })).toBeVisible();
  await page.emulateMedia({ reducedMotion: 'reduce' });
  await page.reload();
  await expect(page.getByRole('button', { name: 'Play carousel' })).toBeVisible();
});

for (const width of [320, 375, 390, 430, 768, 1024, 1280, 1440]) {
  test(`public responsive layout at ${width}px`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    for (const route of ['', 'products', 'custom', 'login', 'about', 'contact']) {
      await page.goto(`./#/${route}`); await expect(page.getByRole('heading', { level: 1 })).toBeVisible();
      await page.locator('[aria-label="Loading content"]').waitFor({ state: 'hidden' }).catch(() => {});
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), route).toBe(true);
      const clipped = await page.locator('.s-header,.s-footer,main').evaluateAll(elements => elements.some(e => e.getBoundingClientRect().right > innerWidth + 1));
      expect(clipped, route).toBe(false);
    }
  });
}
