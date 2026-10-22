import { test, expect, acceptanceApi } from '../fixtures';
import { fillAuthenticator } from '../staffAuth';

const managementEntry = 'https://management.example.test/Amaara-Creations/login';

test('one storefront staff sign-in opens the configured management entry and restores the dashboard without repeating credentials', async ({ page, fixture }) => {
  let navigation: { url: string; method: string; body: string | null } | undefined;
  let sharedSignIns = 0; let additionalStaffSignIns = 0;
  page.on('request', request => {
    if (request.method() !== 'POST') return;
    if (request.url().endsWith('/auth/sign-in')) sharedSignIns++;
    if (request.url().endsWith('/auth/staff/login')) additionalStaffSignIns++;
  });
  // This HTTPS host stands in for the deployment destination. The real staff
  // application and its MFA/session checks are exercised by the staff project.
  await page.route('https://management.example.test/**', async route => {
    const request = route.request();
    navigation = { url: request.url(), method: request.method(), body: request.postData() };
    await route.fulfill({ contentType: 'text/html', body: '<h1>Separate management sign-in destination</h1>' });
  });
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await fillAuthenticator(page);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page).toHaveURL(managementEntry);
  expect(navigation).toEqual({ url: managementEntry, method: 'GET', body: null });
  expect((await page.context().cookies()).some(cookie => cookie.name === 'amaara_refresh_staff' && cookie.httpOnly && cookie.path === '/api/v1/auth/staff')).toBe(true);
  const restored = await page.request.post(acceptanceApi + '/api/v1/auth/refresh');
  expect(restored.ok()).toBeTruthy();
  const token = (await restored.json()).token;
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities', { headers: { Authorization: 'Bearer ' + token } })).status()).toBe(403);
  expect((await page.request.get(acceptanceApi + '/api/v1/auth/staff/session', { headers: { Authorization: 'Bearer ' + token } })).status()).toBe(403);
  // Load the real local management app instead of the intercepted deployment
  // host, using the already issued cookie and without entering another factor.
  await page.goto('http://127.0.0.1:5179/Amaara-Creations/login');
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.getByText('Total Orders', { exact: true })).toBeVisible();
  expect(sharedSignIns).toBe(1); expect(additionalStaffSignIns).toBe(0);
  await page.reload();
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
});

test('customer login ignores forged browser roles and retains the customer profile destination', async ({ page, fixture }) => {
  await page.goto('./login');
  await page.evaluate(() => { localStorage.setItem('role', 'SuperAdmin'); localStorage.setItem('roles', '["Admin"]'); });
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page).toHaveURL(/\/profile$/);
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: /admin|management|studio operations/i })).toHaveCount(0);
});

test('invalid staff credentials stay at the storefront login without creating a management session', async ({ page, fixture }) => {
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
  await page.getByLabel('Password', { exact: true }).fill('Deliberately-wrong-test-password');
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Unable to sign in');
  await expect(page).toHaveURL(/\/login$/);
  expect(new URL(page.url()).hostname).toBe('127.0.0.1');
  expect((await page.context().cookies()).some(cookie => cookie.name === 'amaara_refresh_staff')).toBe(false);
});
