import { test, expect, acceptanceApi } from '../fixtures';
import { fillAuthenticator } from '../staffAuth';

test('verified management sign-in reaches the dashboard and restores it at login and after refresh', async ({ page, fixture }) => {
  await page.goto('./login');
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await page.screenshot({ path: `../.runtime/admin-auth-login-${process.env.AMAARA_E2E_PRODUCTION === '1' ? 'production' : 'development'}.png` });
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await fillAuthenticator(page);
  await page.getByRole('button', { name: /^(Sign In|Verify and sign in)$/ }).click();
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.getByRole('article', { name: 'Total orders', exact: true })).toBeVisible();
  await expect(page.getByRole('alert')).toHaveCount(0);
  await page.screenshot({ path: `../.runtime/admin-auth-dashboard-${process.env.AMAARA_E2E_PRODUCTION === '1' ? 'production' : 'development'}.png` });
  await page.reload();
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.getByRole('article', { name: 'Total orders', exact: true })).toBeVisible();
  await page.goto('./login');
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.getByRole('article', { name: 'Total orders', exact: true })).toBeVisible();
  const cookie = (await page.context().cookies(acceptanceApi + '/api/v1/auth/staff/refresh')).find(cookie => cookie.name === 'amaara_refresh_staff');
  expect(cookie?.httpOnly).toBe(true); expect(cookie?.path).toBe('/api/v1/auth/staff');
  expect(await page.evaluate(() => Object.keys(localStorage).filter(key => /token|role/i.test(key)))).toEqual([]);
});

test('a restored identity cannot load operational modules before server permission verification', async ({ page, fixture }) => {
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code } })).ok()).toBeTruthy();
  let release!: () => void; const gate = new Promise<void>(resolve => { release = resolve; });
  let operationalLoads = 0;
  page.on('request', request => { if (/AdminRoutes|DashboardPage|\/admin\/reports/.test(request.url())) operationalLoads++; });
  await page.route('**/api/v1/auth/staff/session', async route => { const response = await route.fetch(); await gate; try { await route.fulfill({ response }); } catch { /* intentionally aborted on navigation */ } });
  await page.goto('./admin/dashboard');
  try {
    await expect(page.getByRole('status').filter({ hasText: 'Verifying your session' })).toBeVisible();
    await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toHaveCount(0);
    expect(operationalLoads).toBe(0);
  } finally { release(); }
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
});

test('customer and management cookies coexist and management logout leaves shopping signed in', async ({ page, fixture, context }) => {
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.customerEmail, password: fixture.password } })).ok()).toBeTruthy();
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password); await fillAuthenticator(page);
  await page.getByRole('button', { name: /^(Sign In|Verify and sign in)$/ }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  const shop = await context.newPage(); await shop.goto('http://127.0.0.1:5178/Amaara-Creations/profile');
  await expect(shop.getByRole('heading', { level: 1, name: 'Your profile', exact: true })).toBeVisible();
  await page.getByRole('button', { name: /^Open user menu/ }).click();
  await page.getByRole('menuitem', { name: 'Sign out / reauthenticate', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await shop.reload(); await expect(shop.getByRole('heading', { level: 1, name: 'Your profile', exact: true })).toBeVisible();
  await page.goto('./admin/dashboard'); await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toHaveCount(0);
  await shop.close();
});

test('customer credentials, missing MFA and forged browser roles cannot grant management access', async ({ page, fixture }) => {
  await page.goto('./login');
  await page.evaluate(() => { localStorage.setItem('role', 'SuperAdmin'); localStorage.setItem('roles', JSON.stringify(['Admin'])); });
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: /^(Sign In|Verify and sign in)$/ }).click();
  await expect(page).toHaveURL(/\/profile$/);
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
  await page.goto('http://127.0.0.1:5178/Amaara-Creations/login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: /^(Sign In|Verify and sign in)$/ }).click();
  await expect(page.getByRole('heading', { name: /Verify it/ })).toBeVisible();
  await expect(page.getByLabel('Password', { exact: true })).toHaveCount(0);
  expect((await page.context().cookies()).some(cookie => cookie.name === 'amaara_refresh_staff')).toBe(false);
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
});

test('specialist staff land only in a permitted workspace and cannot enter owner operations', async ({ page, request }) => {
  const response = await request.post(acceptanceApi + '/__fixture/operations'); expect(response.ok()).toBeTruthy(); const fixture = await response.json();
  await page.goto('./login'); await page.getByLabel('Email Address', { exact: true }).fill(fixture.supportEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.basic.password); await fillAuthenticator(page);
  await page.getByRole('button', { name: /^(Sign In|Verify and sign in)$/ }).click(); await expect(page).toHaveURL(/\/admin\/customers$/);
  await page.goto('./admin/staff'); await expect(page.getByRole('heading', { name: 'Access denied', exact: true })).toBeVisible();
  const session = await page.request.post(acceptanceApi + '/api/v1/auth/staff/refresh'); expect(session.ok()).toBeTruthy(); const token = (await session.json()).token;
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/staff', { headers: { Authorization: 'Bearer ' + token } })).status()).toBe(403);
});
