import { test, expect, acceptanceApi } from './fixtures';

test.describe('Custom Builder live custom sticker workflow', () => {
  test.beforeEach(async ({ context, fixture }) => {
    const response = await context.request.post(`${acceptanceApi}/api/v1/auth/login`, {
      data: { email: fixture.adminEmail, password: fixture.password },
    });
    expect(response.ok()).toBeTruthy();
  });

  test('saves, reloads, uploads private artwork and adds a custom item to cart', async ({ page }) => {
    const pageErrors: string[] = [];
    page.on('pageerror', (error) => pageErrors.push(error.message));

    await page.goto('./custom');
    await expect(page.getByRole('heading', { level: 1, name: 'Build a custom sticker' })).toBeVisible();
    await expect(page.getByText('Custom item subtotal')).toBeVisible();

    const designName = `Browser Custom Builder ${Date.now()}`;
    await page.getByLabel('Design name').fill(designName);
    await page.getByLabel('Sticker text (optional)').fill('Browser verified');
    await page.getByLabel(/^Width/).fill('7');
    await page.getByLabel(/^Height/).fill('5');
    await page.getByLabel('Quantity').fill('4');
    await expect(page.getByText('Custom item subtotal')).toBeVisible();

    const artworkInput = page.getByLabel('Artwork (JPEG, PNG or WebP)');
    const originalInput = await artworkInput.elementHandle();
    let releaseDetails!: () => void;
    const detailsGate = new Promise<void>(resolve => { releaseDetails = resolve; });
    await page.route('**/api/v1/custom-designs/*', async route => {
      if (route.request().method() !== 'GET') return route.continue();
      const response = await route.fetch();
      await detailsGate;
      await route.fulfill({ response });
    });
    try {
      await page.getByRole('button', { name: 'Save design' }).click();
      await expect(page).toHaveURL(/\/custom\/\d+$/);
      await expect(page.getByRole('status').filter({ hasText: 'Custom design saved.' })).toBeVisible();
      await expect(artworkInput).toBeEnabled();
      expect(await originalInput!.evaluate(input => input.isConnected), 'Save-to-edit keeps the artwork input mounted').toBe(true);
      const uploaded = page.waitForResponse(response => response.url().endsWith('/artwork') && response.request().method() === 'POST');
      await artworkInput.setInputFiles({
        name: 'browser-smoke.png',
        mimeType: 'image/png',
        buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64'),
      });
      expect((await uploaded).ok()).toBeTruthy();
      await expect(page.getByRole('status').filter({ hasText: 'Artwork uploaded securely.' })).toBeVisible();
      await expect(page.getByText('browser-smoke.png')).toBeVisible();
      await expect(artworkInput).toBeDisabled();
    } finally { releaseDetails(); }
    await expect(artworkInput).toBeEnabled();
    await expect(page.getByRole('status').filter({ hasText: 'Artwork uploaded securely.' })).toBeVisible();
    await page.reload();
    await expect(page.getByText('browser-smoke.png')).toBeVisible();
    await expect(page.getByLabel('Design name')).toHaveValue(designName);

    await page.getByRole('button', { name: 'Save and add to cart' }).click();
    await expect(page).toHaveURL(/\/cart$/);
    await expect(page.getByRole('heading', { level: 1, name: 'Shopping cart' })).toBeVisible();
    await expect(page.getByRole('link', { name: designName, exact: true })).toBeVisible();
    expect(pageErrors).toEqual([]);
  });

  test('remains operable without horizontal overflow at required widths', async ({ page }) => {
    for (const width of [375, 430, 768, 1024, 1440]) {
      await page.setViewportSize({ width, height: 900 });
      await page.goto('./custom');
      await expect(page.getByRole('heading', { level: 1, name: 'Build a custom sticker' })).toBeVisible();
      await expect(page.getByText('Custom item subtotal')).toBeVisible();
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), `${width}px`).toBe(true);
      await expect(page.getByLabel('Design name')).toBeEditable();
      await expect(page.getByLabel('Artwork (JPEG, PNG or WebP)')).toBeEnabled();
    }
  });

  test('exposes the builder and production admin screens to an authorised administrator', async ({ page }) => {
    await page.goto('./admin/custom-builder');
    await expect(page.getByRole('heading', { name: 'Custom builder configuration' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Test quote' })).toBeEnabled();
    await expect(page.getByText('Version history')).toBeVisible();

    await page.goto('./admin/custom-designs');
    await expect(page.getByRole('heading', { name: 'Custom design production' })).toBeVisible();
    await expect(page.getByLabel('Search order or customer')).toBeEditable();
    await expect(page.getByLabel('Proof state')).toBeEnabled();
    await expect(page.getByLabel('Production state')).toBeEnabled();
  });
});
