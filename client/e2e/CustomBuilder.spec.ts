import { test, expect } from '@playwright/test';

const adminEmail = process.env.CustomBuilder_ADMIN_EMAIL;
const adminPassword = process.env.CustomBuilder_ADMIN_PASSWORD;

test.describe('Custom Builder live custom sticker workflow', () => {
  test.skip(!adminEmail || !adminPassword, 'Set CustomBuilder_ADMIN_EMAIL and CustomBuilder_ADMIN_PASSWORD for the live Custom Builder smoke test.');

  test.beforeEach(async ({ context }) => {
    const response = await context.request.post('http://localhost:5192/api/v1/auth/login', {
      data: { email: adminEmail, password: adminPassword },
    });
    expect(response.ok()).toBeTruthy();
  });

  test('saves, reloads, uploads private artwork and adds a custom item to cart', async ({ page }) => {
    const pageErrors: string[] = [];
    page.on('pageerror', (error) => pageErrors.push(error.message));

    await page.goto('./#/custom');
    await expect(page.getByRole('heading', { level: 1, name: 'Build a custom sticker' })).toBeVisible();
    await expect(page.getByText('Custom item subtotal')).toBeVisible();

    const designName = `Browser Custom Builder ${Date.now()}`;
    await page.getByLabel('Design name').fill(designName);
    await page.getByLabel('Sticker text (optional)').fill('Browser verified');
    await page.getByLabel(/^Width/).fill('7');
    await page.getByLabel(/^Height/).fill('5');
    await page.getByLabel('Quantity').fill('4');
    await expect(page.getByText('Custom item subtotal')).toBeVisible();

    await page.getByRole('button', { name: 'Save design' }).click();
    await expect(page).toHaveURL(/#\/custom\/\d+$/);
    await expect(page.getByRole('status').filter({ hasText: 'Custom design saved.' })).toBeVisible();

    const artworkInput = page.getByLabel('Artwork (JPEG, PNG or WebP)');
    await expect(artworkInput).toBeEnabled();
    await artworkInput.setInputFiles({
      name: 'browser-smoke.png',
      mimeType: 'image/png',
      buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64'),
    });
    await expect(page.getByRole('status').filter({ hasText: 'Artwork uploaded securely.' })).toBeVisible();
    await expect(page.getByText('browser-smoke.png')).toBeVisible();

    await page.getByRole('button', { name: 'Save and add to cart' }).click();
    await expect(page).toHaveURL(/#\/cart$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Shopping cart' })).toBeVisible();
    await expect(page.getByRole('link', { name: designName, exact: true })).toBeVisible();
    expect(pageErrors).toEqual([]);
  });

  test('remains operable without horizontal overflow at required widths', async ({ page }) => {
    for (const width of [375, 430, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      await page.goto('./#/custom');
      await expect(page.getByRole('heading', { level: 1, name: 'Build a custom sticker' })).toBeVisible();
      await expect(page.getByText('Custom item subtotal')).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), `${width}px`).toBe(true);
      await expect(page.getByLabel('Design name')).toBeEditable();
      await expect(page.getByLabel('Artwork (JPEG, PNG or WebP)')).toBeEnabled();
    }
  });

  test('exposes the builder and production admin screens to an authorised administrator', async ({ page }) => {
    await page.goto('./#/admin/custom-builder');
    await expect(page.getByRole('heading', { name: 'Custom builder configuration' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Test quote' })).toBeEnabled();
    await expect(page.getByText('Version history')).toBeVisible();

    await page.goto('./#/admin/custom-designs');
    await expect(page.getByRole('heading', { name: 'Custom design production' })).toBeVisible();
    await expect(page.getByLabel('Search')).toBeEditable();
    await expect(page.getByLabel('Proof status')).toBeEnabled();
    await expect(page.getByLabel('Production')).toBeEnabled();
  });
});
