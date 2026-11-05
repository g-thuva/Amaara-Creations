import { chromium } from '@playwright/test';
import { test, expect, acceptanceApi, localManagementNavigation, storefrontUrl, managementUrl, type Page } from '../fixtures';
import { fillAuthenticator } from '../staffAuth';
import { resolve } from 'node:path';

const shop = storefrontUrl;
const staffSite = managementUrl;
async function signIn(page: Page, fixture: { customerEmail: string; adminEmail: string; password: string }, staff = false) {
  await page.goto((staff ? staffSite : shop) + 'login');
  await page.getByLabel('Email Address', { exact: true }).fill(staff ? fixture.adminEmail : fixture.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  if (staff) await fillAuthenticator(page);
  await page.getByRole('button', { name: /^(Sign In|Verify and sign in)$/ }).click();
  await expect(page.getByRole('heading', { level: 1, name: staff ? 'Dashboard' : 'Your profile', exact: true })).toBeVisible({ timeout: 15000 });
}

test('customer reload, a new tab and multi-tab logout use the same valid server session', async ({ page, fixture, context }) => {
  await signIn(page, fixture); await page.reload();
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  const second = await context.newPage(); await second.goto(shop + 'profile');
  await expect(second.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  await Promise.all([page.reload(), second.reload()]);
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  await expect(second.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  await page.getByRole('navigation', { name: 'Account navigation' }).getByRole('button', { name: 'Sign out', exact: true }).click();
  await expect(second.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await second.reload(); await expect(second.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  expect((await second.request.post(acceptanceApi + '/api/v1/auth/refresh')).status()).toBe(401);
  await second.close();
});

for (const staff of [false, true]) {
  test(`${staff ? 'admin MFA' : 'customer'} persistent cookies restore after a complete browser restart`, async ({ fixture }) => {
    const profile = resolve('../.runtime', `session-profile-${crypto.randomUUID()}`);
    const launch = () => chromium.launchPersistentContext(profile, { channel: process.platform === 'win32' ? 'chrome' : undefined, headless: true });
    let context = await launch();
    try {
      await localManagementNavigation(context);
      const page = context.pages()[0]; await signIn(page, fixture, staff);
      const cookies = await context.cookies(acceptanceApi + (staff ? '/api/v1/auth/staff/refresh' : '/api/v1/auth/refresh'));
      const cookie = cookies.find(value => value.name === (staff ? 'amaara_refresh_staff' : 'amaara_refresh'));
      expect(cookie?.httpOnly).toBe(true); expect(cookie?.expires).toBeGreaterThan(Date.now() / 1000);
      await context.close(); context = await launch(); await localManagementNavigation(context);
      const restored = context.pages()[0]; await restored.goto((staff ? staffSite : shop) + (staff ? 'admin/dashboard' : 'profile'));
      await expect(restored.getByRole('heading', { level: 1, name: staff ? 'Dashboard' : 'Your profile', exact: true })).toBeVisible({ timeout: 15000 });
      expect(await restored.evaluate(() => [...Object.entries(localStorage), ...Object.entries(sessionStorage)].some(([key, value]) => /password|refresh.?token|access.?token|totp|recovery.?code/i.test(key + value)))).toBe(false);
      expect(await restored.evaluate(() => document.cookie.includes('amaara_refresh'))).toBe(false);
    } finally { await context.close(); }
  });

  test(`${staff ? 'admin' : 'customer'} expired access renews once and safely retries concurrent requests without navigation`, async ({ page, fixture }) => {
    await signIn(page, fixture, staff); const original = page.url();
    const issued = await page.request.post(acceptanceApi + '/__fixture/session', { data: { email: staff ? fixture.adminEmail : fixture.customerEmail, management: staff, action: 'expire-access' } });
    expect(issued.ok()).toBeTruthy(); const token = (await issued.json()).token;
    let renewals = 0; page.on('request', request => { if (request.url().endsWith(staff ? '/auth/staff/refresh' : '/auth/refresh')) renewals++; });
    const result = await page.evaluate(async ({ token, staff, productId }) => {
      // Install a genuinely server-signed expired access token into its normal
      // in-memory slot; all refresh/authentication responses come from the API.
      // @ts-expect-error Vite serves this local source module in acceptance mode.
      const session = await import('/Amaara-Creations/src/shared/api/authSession.js');
      // @ts-expect-error Vite serves this local source module in acceptance mode.
      const { default: api } = await import('/Amaara-Creations/src/shared/api/httpClient.js');
      session.setAccessToken(token);
      const responses = await Promise.all([api.get('/auth/me'), api.get('/auth/sessions'), staff ? api.post('/auth/activity') : api.post('/cart', { productId, quantity: 1 })]);
      const quantity = staff ? null : (await api.get('/cart')).data.items.find(item => item.productId === productId)?.quantity;
      return { statuses: responses.map(response => response.status), quantity };
    }, { token, staff, productId: fixture.products[0] });
    expect(result.statuses.every(status => status >= 200 && status < 300)).toBe(true);
    if (!staff) expect(result.quantity).toBe(1);
    expect(renewals).toBe(1); expect(page.url()).toBe(original);
    await expect(page.getByRole('heading', { level: 1, name: staff ? 'Dashboard' : 'Your profile', exact: true })).toBeVisible({ timeout: 15000 });
  });

  for (const action of ['expire-idle', 'expire-absolute', 'revoke']) {
    test(`${staff ? 'admin' : 'customer'} ${action} requires server-authorized reauthentication`, async ({ page, fixture }) => {
      await signIn(page, fixture, staff);
      const response = await page.request.post(acceptanceApi + '/__fixture/session', { data: { email: staff ? fixture.adminEmail : fixture.customerEmail, management: staff, action } });
      expect(response.ok()).toBeTruthy(); await page.reload();
      await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
      await expect(page.getByRole('heading', { name: staff ? 'Dashboard' : 'Your profile', exact: true })).toHaveCount(0);
      if (staff) {
        await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
        await page.getByLabel('Password', { exact: true }).fill(fixture.password);
        await page.getByRole('button', { name: 'Sign In', exact: true }).click();
        await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
        expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
      }
    });
  }
}

test('temporary restoration failure offers retry without revoking the persistent session', async ({ page, fixture }) => {
  await signIn(page, fixture);
  await page.route('**/api/v1/auth/refresh', route => route.abort('internetdisconnected'));
  await page.reload(); await expect(page.getByRole('heading', { name: 'Unable to check your session', exact: true })).toBeVisible();
  await expect(page.getByText('Your session has not been signed out.', { exact: false })).toBeVisible();
  await page.unroute('**/api/v1/auth/refresh'); await page.getByRole('button', { name: 'Try again', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
});

test('restoration keeps protected content and login hidden until the real session response arrives', async ({ page, fixture }) => {
  await signIn(page, fixture);
  let release!: () => void; const gate = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/v1/auth/refresh', async route => { const response = await route.fetch(); await gate; await route.fulfill({ response }); });
  await page.reload();
  try {
    await expect(page.getByText('Restoring session...', { exact: true })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toHaveCount(0);
    await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toHaveCount(0);
  } finally { release(); }
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
});

test('a failed logout reports that revocation is incomplete and can be retried', async ({ page, fixture }) => {
  await signIn(page, fixture);
  await page.route('**/api/v1/auth/logout', route => route.abort('internetdisconnected'));
  await page.getByRole('navigation', { name: 'Account navigation' }).getByRole('button', { name: 'Sign out', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Sign out could not be completed', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toHaveCount(0);
  await page.unroute('**/api/v1/auth/logout'); await page.getByRole('button', { name: 'Try again', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/refresh')).status()).toBe(401);
  await expect(page.getByText('Your session has expired. Please sign in again.', { exact: true })).toHaveCount(0);
});
