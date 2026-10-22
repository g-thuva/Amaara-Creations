import { test, expect, acceptanceApi } from '../fixtures';
import type { Page } from '@playwright/test';

const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64');
const image = (name = 'private-artwork.png') => ({ name, mimeType: 'image/png', buffer: png });
const artworkInput = (page: Page) => page.getByLabel('Artwork (JPEG, PNG or WebP)');
const uploadedNotice = (page: Page) => page.getByRole('status').filter({ hasText: 'Artwork uploaded securely.' });
async function openBuilder(page: Page) {
  await page.goto('./custom');
  await expect(page.getByText('Custom item subtotal')).toBeVisible();
}
async function saveBuilder(page: Page) {
  await openBuilder(page);
  await page.getByRole('button', { name: 'Save design', exact: true }).click();
  await expect(page).toHaveURL(/\/custom\/\d+$/);
  await expect(artworkInput(page)).toBeEnabled();
  return page.url();
}
async function upload(page: Page, name = 'private-artwork.png') {
  const response = page.waitForResponse(r => r.url().endsWith('/artwork') && r.request().method() === 'POST');
  await artworkInput(page).setInputFiles(image(name));
  expect((await response).ok()).toBeTruthy();
  await expect(uploadedNotice(page)).toBeVisible();
  await expect(page.getByText(name, { exact: true })).toBeVisible();
  await expect(artworkInput(page)).toBeEnabled();
}

test.describe('Private Custom Builder artwork regressions', () => {
  test.beforeEach(async ({ context, fixture }) => {
    const response = await context.request.post(`${acceptanceApi}/api/v1/auth/login`, {
      data: { email: fixture.customerEmail, password: fixture.password },
    });
    expect(response.ok()).toBeTruthy();
  });

  for (const width of [375, 768, 1440]) {
    test(`persists, reopens and removes artwork with keyboard at ${width}px`, async ({ page }) => {
      await page.setViewportSize({ width, height: 900 });
      const savedUrl = await saveBuilder(page);
      await upload(page);
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
      await page.reload();
      await expect(page.getByText('private-artwork.png', { exact: true })).toBeVisible();
      await page.goto('./account/designs');
      await expect(page.getByRole('heading', { level: 1, name: 'Saved designs' })).toBeVisible();
      await page.goto(savedUrl);
      await expect(page.getByText('private-artwork.png', { exact: true })).toBeVisible();
      await page.getByRole('button', { name: 'Remove', exact: true }).focus();
      await page.keyboard.press('Enter');
      await expect(page.getByRole('status').filter({ hasText: 'Artwork removed.' })).toBeVisible();
      await expect(page.getByText('private-artwork.png', { exact: true })).toHaveCount(0);
      await expect(artworkInput(page)).toBeEnabled();
      await page.reload();
      await expect(artworkInput(page)).toBeEnabled();
      await expect(page.getByText('private-artwork.png', { exact: true })).toHaveCount(0);
    });
  }

  test('automatically saves before upload and guards two queued input changes', async ({ page }) => {
    await openBuilder(page);
    let creates = 0, uploads = 0;
    page.on('request', request => {
      if (request.method() !== 'POST') return;
      if (new URL(request.url()).pathname === '/api/v1/custom-designs') creates++;
      if (request.url().endsWith('/artwork')) uploads++;
    });
    await artworkInput(page).evaluate((input: HTMLInputElement, bytes) => {
      const transfer = new DataTransfer();
      transfer.items.add(new File([Uint8Array.from(atob(bytes), char => char.charCodeAt(0))], 'queued.png', { type: 'image/png' }));
      input.files = transfer.files;
      input.dispatchEvent(new Event('change', { bubbles: true }));
      input.dispatchEvent(new Event('change', { bubbles: true }));
    }, png.toString('base64'));
    await expect(page).toHaveURL(/\/custom\/\d+$/);
    await expect(uploadedNotice(page)).toBeVisible();
    await expect(page.getByText('queued.png', { exact: true })).toBeVisible();
    await expect(artworkInput(page)).toBeEnabled();
    expect(creates).toBe(1); expect(uploads).toBe(1);
    await page.reload();
    await expect(page.getByText('queued.png', { exact: true })).toHaveCount(1);
  });

  const failures = [
    { label: 'unsupported format', file: { name: 'invalid.svg', mimeType: 'image/svg+xml', buffer: Buffer.from('<svg/>') }, message: 'Artwork must be a JPEG, PNG or WebP image.' },
    { label: 'too-large file', file: { name: 'oversize.png', mimeType: 'image/png', buffer: Buffer.alloc(5_242_881) }, message: 'Artwork is too large. Choose a smaller image and try again.' },
    { label: 'corrupt image', file: { name: 'corrupt.png', mimeType: 'image/png', buffer: Buffer.concat([png.subarray(0, -5), Buffer.from([0, 0, 0, 0, 0])]) }, message: 'PNG file is incomplete or corrupted.' },
    { label: 'API 400', status: 400, message: 'Choose a valid image and try again.' },
    { label: 'API 401', status: 401, message: 'Your session has ended. Please sign in again.' },
    { label: 'API 403', status: 403, message: 'You do not have access to this action.' },
    { label: 'API 404', status: 404, message: 'This item is no longer available.' },
    { label: 'API 500', status: 500, message: 'We could not complete your request. Please try again.' },
  ];
  for (const failure of failures) {
    test(`recovers from ${failure.label} without losing existing artwork or claiming success`, async ({ page }) => {
      await saveBuilder(page);
      await upload(page, 'existing.png');
      await page.getByRole('button', { name: 'Dismiss notification' }).click();
      if ('status' in failure) await page.route('**/api/v1/custom-designs/*/artwork', route => route.fulfill({
        status: failure.status, json: { errors: { file: [failure.message] } },
      }));
      await artworkInput(page).setInputFiles('file' in failure ? failure.file : image('rejected.png'));
      await expect(page.locator('.s-custom-page [role=alert]')).toContainText(failure.message);
      await expect(uploadedNotice(page)).toHaveCount(0);
      await expect(page.getByText('existing.png', { exact: true })).toBeVisible();
      await expect(page.getByText('rejected.png', { exact: true })).toHaveCount(0);
      await expect(artworkInput(page)).toBeEnabled();
      await expect(page.getByLabel('Design name')).toBeEditable();
      if ('status' in failure) await page.unroute('**/api/v1/custom-designs/*/artwork');
      await upload(page, 'recovery.png');
      await expect(page.locator('.s-custom-page [role=alert]')).toHaveCount(0);
      await page.reload();
      await expect(page.getByText('existing.png', { exact: true })).toBeVisible();
      await expect(page.getByText('recovery.png', { exact: true })).toBeVisible();
    });
  }

  test('keeps persisted artwork and success visible when detail refresh fails', async ({ page }) => {
    await saveBuilder(page);
    await page.route('**/api/v1/custom-designs/*', route => route.request().method() === 'GET'
      ? route.fulfill({ status: 503, json: { title: 'Simulated detail read failure' } }) : route.continue());
    await upload(page);
    await expect(page.locator('.s-custom-page [role=alert]')).toBeVisible();
    await expect(uploadedNotice(page)).toBeVisible();
    await page.unroute('**/api/v1/custom-designs/*');
    await page.reload();
    await expect(page.getByText('private-artwork.png', { exact: true })).toBeVisible();
    await expect(page.locator('.s-custom-page [role=alert]')).toHaveCount(0);
  });

  test('rejects a late artwork detail response after switching designs', async ({ page }) => {
    await saveBuilder(page);
    const secondId = await page.evaluate(async () => {
      const { customDesignApi } = await import('/Amaara-Creations/src/features/custom-design/customDesignApi.js');
      const first = await customDesignApi.getDesign(location.pathname.split('/').pop());
      return (await customDesignApi.createDesign({ ...first, name: 'Second private design' })).id;
    });
    let release!: () => void, received!: () => void;
    const gate = new Promise<void>(resolve => { release = resolve; });
    const readStarted = new Promise<void>(resolve => { received = resolve; });
    const firstId = page.url().split('/').pop();
    await page.route(`**/api/v1/custom-designs/${firstId}`, async route => {
      if (route.request().method() !== 'GET') return route.continue();
      const response = await route.fetch(); received(); await gate; await route.fulfill({ response });
    });
    try {
      await artworkInput(page).setInputFiles(image('first-design.png'));
      await readStarted;
      await expect(uploadedNotice(page)).toBeVisible();
      await page.evaluate(id => { history.pushState(null, '', `/Amaara-Creations/custom/${id}`); dispatchEvent(new PopStateEvent('popstate')); }, secondId);
      await expect(page.getByLabel('Design name')).toHaveValue('Second private design');
    } finally { release(); }
    await expect(artworkInput(page)).toBeEnabled();
    await expect(page.getByLabel('Design name')).toHaveValue('Second private design');
    await expect(page.getByText('first-design.png', { exact: true })).toHaveCount(0);
  });

  test('streams artwork only to its owner or admin and exposes no storage path', async ({ page, request, fixture }) => {
    await saveBuilder(page); await upload(page);
    const designId = page.url().split('/').pop();
    const ownerLogin = await request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } });
    const ownerToken = (await ownerLogin.json()).token;
    const details = await request.get(`${acceptanceApi}/api/v1/custom-designs/${designId}`, { headers: { Authorization: `Bearer ${ownerToken}` } });
    expect(details.ok()).toBeTruthy();
    const dto = await details.json(); const asset = dto.assets[0];
    expect(asset.downloadUrl).toMatch(/^\/api\/v1\/custom-designs\/\d+\/assets\/\d+$/);
    expect(asset).not.toHaveProperty('storageKey'); expect(JSON.stringify(dto)).not.toMatch(/wwwroot|App_Data|[A-Z]:\\/);
    expect((await request.get(`${acceptanceApi}${asset.downloadUrl}`, { headers: { Authorization: `Bearer ${ownerToken}` } })).status()).toBe(200);
    expect((await request.get(`${acceptanceApi}${asset.downloadUrl}`)).status()).toBe(401);
    const other = await request.post(`${acceptanceApi}/__fixture/new`); const otherFixture = await other.json();
    const otherLogin = await request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: otherFixture.customerEmail, password: otherFixture.password } });
    const otherToken = (await otherLogin.json()).token;
    const otherHeaders = { Authorization: `Bearer ${otherToken}` };
    expect((await request.get(`${acceptanceApi}${asset.downloadUrl}`, { headers: otherHeaders })).status()).toBe(404);
    expect((await request.get(`${acceptanceApi}/api/v1/custom-designs/${designId}`, { headers: otherHeaders })).status()).toBe(404);
    expect((await request.delete(`${acceptanceApi}${asset.downloadUrl}`, { headers: otherHeaders })).status()).toBe(404);
    expect((await request.post(`${acceptanceApi}/api/v1/custom-designs/${designId}/artwork`, { headers: otherHeaders,
      multipart: { file: { name: 'intruder.png', mimeType: 'image/png', buffer: png } } })).status()).toBe(404);
    const adminLogin = await request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.adminEmail, password: fixture.password } });
    expect(adminLogin.status()).toBe(202);
    expect((await adminLogin.json()).token).toBeUndefined();
    expect((await request.get(`${acceptanceApi}${asset.downloadUrl}`)).status()).toBe(401);
    const code = (await (await request.get(`${acceptanceApi}/__fixture/authenticator-code`)).json()).code;
    const steppedUp = await request.post(`${acceptanceApi}/api/v1/auth/staff/login`, { data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code } });
    const steppedUpToken = (await steppedUp.json()).token;
    expect((await request.get(`${acceptanceApi}${asset.downloadUrl}`, { headers: { Authorization: `Bearer ${steppedUpToken}` } })).status()).toBe(200);
  });
});
