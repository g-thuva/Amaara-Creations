import { test, expect, acceptanceApi, type Page } from '../fixtures';

async function openDashboard(page: Page, fixture: { adminEmail: string; password: string }) {
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/login', {
    data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code },
  })).ok()).toBe(true);
  await page.goto('./admin/dashboard');
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.getByText('Total Orders', { exact: true })).toBeVisible();
}

test('sidebar and header stay mounted through a cold page import and slow background permission checks', async ({ page, fixture }) => {
  await openDashboard(page, fixture);
  const sidebar = page.locator('#admin-navigation');
  await sidebar.evaluate(node => { node.dataset.navigationMount = 'original'; });
  let releaseChunk!: () => void; const chunkGate = new Promise<void>(resolve => { releaseChunk = resolve; });
  let releaseSession!: () => void; const sessionGate = new Promise<void>(resolve => { releaseSession = resolve; });
  let chunkRequested = false; let sessionChecks = 0;
  await page.route(/\/ProductsPage[^/]*\.(?:jsx|js)(?:\?|$)/, async route => {
    chunkRequested = true;
    const response = await route.fetch(); await chunkGate;
    try { await route.fulfill({ response }); } catch { /* A later route can discard this import. */ }
  });
  await page.route('**/api/v1/auth/staff/session', async route => {
    sessionChecks++;
    const response = await route.fetch(); await sessionGate;
    try { await route.fulfill({ response }); } catch { /* Superseded checks are aborted. */ }
  });
  try {
    await sidebar.getByRole('link', { name: 'Products', exact: true }).click();
    await expect.poll(() => chunkRequested).toBe(true);
    await expect(sidebar).toBeVisible();
    await expect(sidebar).toHaveAttribute('data-navigation-mount', 'original');
    await expect(page.locator('.admin-header')).toBeVisible();
    // React may retain the previous content during its navigation transition.
    // Any Suspense fallback belongs inside this still-visible content region.
    await expect(page.locator('.admin-content')).toBeVisible();
    await expect(page.getByText('Loading your workspace...', { exact: true })).toHaveCount(0);
    await expect(page.getByText('Verifying your session...', { exact: true })).toHaveCount(0);
    releaseChunk();
    await expect(page.locator('.admin-content').getByRole('heading', { name: 'Product Management', exact: true })).toBeVisible();
    await expect.poll(() => sessionChecks).toBeGreaterThan(0);
    await expect(sidebar).toHaveAttribute('data-navigation-mount', 'original');
    await page.screenshot({ path: `../.runtime/admin-navigation-stable-${process.env.AMAARA_E2E_PRODUCTION === '1' ? 'production' : 'development'}.png` });
    // Navigation remains usable even before the previous permission check finishes.
    await sidebar.getByRole('link', { name: 'Orders', exact: true }).click();
    await expect(page.locator('.admin-content').getByRole('heading', { name: 'Order management', exact: true })).toBeVisible();
    await expect(sidebar).toHaveAttribute('data-navigation-mount', 'original');
    releaseChunk(); releaseSession();
    await expect(page).toHaveURL(/\/admin\/orders$/);
    await expect(page.locator('.admin-content').getByRole('heading', { name: 'Order management', exact: true })).toBeVisible();
    await sidebar.getByRole('link', { name: 'Products', exact: true }).click();
    await expect(page.locator('.admin-content').getByRole('heading', { name: 'Product Management', exact: true })).toBeVisible();
    await expect(sidebar).toHaveAttribute('data-navigation-mount', 'original');
  } finally { releaseChunk(); releaseSession(); }
});

test('a revoked session is still rejected by background revalidation during navigation', async ({ page, fixture }) => {
  await openDashboard(page, fixture);
  let release!: () => void; const gate = new Promise<void>(resolve => { release = resolve; });
  let checking = false;
  await page.route('**/api/v1/auth/staff/session', async route => {
    checking = true; await gate;
    try { await route.fulfill({ response: await route.fetch() }); } catch { /* Authentication expiry can abort the request. */ }
  });
  try {
    await page.getByRole('navigation', { name: 'Administration', exact: true }).getByRole('link', { name: 'Orders', exact: true }).click();
    await expect.poll(() => checking).toBe(true);
    expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/logout')).ok()).toBe(true);
    release();
    await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
    await expect(page.locator('#admin-navigation')).toHaveCount(0);
    expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
  } finally { release(); }
});
