import { test, expect, acceptanceApi } from '../fixtures';

test('stopping an artwork transfer reconciles saved state and permits a genuine retry', async ({ page, fixture }) => {
  expect((await page.request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } })).ok()).toBeTruthy();
  await page.goto('./custom');
  await expect(page.getByText('Custom item subtotal', { exact: true })).toBeVisible();
  let release!: () => void;
  const gate = new Promise<void>(resolve => { release = resolve; });
  const pattern = '**/api/v1/custom-designs/*/artwork';
  await page.route(pattern, async route => {
    await gate;
    try { await route.fulfill({ status: 503, contentType: 'application/json', body: JSON.stringify({ message: 'Test transfer stopped' }) }); }
    catch { /* The browser has intentionally aborted this intercepted request. */ }
  });
  await page.getByLabel('Artwork (JPEG, PNG or WebP)', { exact: true }).setInputFiles({ name: 'retry-private.png', mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64') });
  await expect(page.getByRole('progressbar', { name: 'Artwork transfer', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Stop upload', exact: true }).click();
  await expect(page.getByRole('button', { name: 'Retry artwork upload', exact: true })).toBeVisible();
  await expect(page.getByText('Artwork uploaded securely.', { exact: true })).toHaveCount(0);
  release(); await page.unroute(pattern);
  await page.getByRole('button', { name: 'Retry artwork upload', exact: true }).click();
  await expect(page.getByText('retry-private.png', { exact: true })).toBeVisible();
  await expect(page.getByText('Artwork uploaded securely.', { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByAltText('Selected artwork preview')).toHaveAttribute('src', /^blob:/);
});
