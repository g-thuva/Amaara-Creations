import { test, expect, acceptanceApi, storefrontUrl, managementUrl, type Page } from '../fixtures';
import { fillAuthenticator } from '../staffAuth';

const shop = storefrontUrl;
const staff = managementUrl;
async function credentials(page: Page, site: string, email: string, password: string) {
  await page.goto(site + 'login');
  await page.getByLabel('Email Address', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
}
async function completeMfa(page: Page) {
  await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
  await fillAuthenticator(page);
  const result = page.waitForResponse(response => response.url().endsWith('/auth/sign-in/verify'));
  await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click();
  expect((await result).status()).toBe(200);
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
}

test('admin MFA creates management access, succeeds on dashboard APIs and survives reload without customer identity', async ({ page, fixture }) => {
  const evidence: string[] = [];
  const clientErrors: string[] = [];
  page.on('pageerror', error => clientErrors.push(error.name));
  page.on('response', response => {
    if (/\/auth\/staff\/(sign-in(?:\/verify)?|session|refresh)$|\/admin\/dashboard$/.test(response.url()))
      evidence.push(`${new URL(response.url()).pathname} ${response.status()}`);
  });
  await credentials(page, staff, fixture.adminEmail, fixture.password);
  await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
  await completeMfa(page);
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await expect(page.getByRole('article', { name: 'Total orders', exact: true })).toBeVisible();
  await page.screenshot({ path: '../.runtime/mfa-fix-dashboard.png' });
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  const refresh = await page.request.post(acceptanceApi + '/api/v1/auth/staff/refresh');
  expect(refresh.status()).toBe(200);
  const auth = await refresh.json();
  expect(auth.user.email === fixture.adminEmail).toBe(true);
  const headers = { Authorization: 'Bearer ' + auth.token };
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities', { headers })).status()).toBe(200);
  expect((await page.request.get(acceptanceApi + '/api/v1/cart', { headers })).status()).toBe(403);
  const cookies = await page.context().cookies();
  expect(cookies.some(cookie => cookie.name === 'amaara_refresh')).toBe(false);
  expect(cookies.some(cookie => cookie.name === 'amaara_refresh_staff' && cookie.httpOnly)).toBe(true);
  await page.goto(shop + 'profile');
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  expect(clientErrors).toEqual([]);
  console.log('Admin journey HTTP evidence:', evidence.join(', '));
});

test('admin and specialist use the shared login and mandatory MFA without a customer session', async ({ page, fixture, request }) => {
  const operations = await (await request.post(acceptanceApi + '/__fixture/operations')).json();
  for (const [email, password] of [[fixture.adminEmail, fixture.password], [operations.supportEmail, operations.basic.password]]) {
    await credentials(page, shop, email, password);
    await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
    expect((await page.request.post(acceptanceApi + '/api/v1/auth/refresh')).status()).toBe(401);
    expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/refresh')).status()).toBe(401);
    await fillAuthenticator(page);
    await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click();
    await expect(page).toHaveURL(/\/admin\/(dashboard|customers)$/);
    expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/refresh')).status()).toBe(200);
    expect((await page.request.get(acceptanceApi + '/api/v1/cart')).status()).toBe(401);
    expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/logout')).status()).toBe(200);
  }
});

test('customer credentials cannot initiate privileged MFA or open admin routes', async ({ page, fixture }) => {
  await credentials(page, staff, fixture.customerEmail, fixture.password);
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  await expect(page.getByLabel('Authentication code', { exact: true })).toHaveCount(0);
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/refresh')).status()).toBe(401);
  await page.goto(staff + 'admin/dashboard');
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toHaveCount(0);
});

test('normal customer password login needs no admin MFA and cannot call admin APIs', async ({ page, fixture }) => {
  await credentials(page, shop, fixture.customerEmail, fixture.password);
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  const refresh = await page.request.post(acceptanceApi + '/api/v1/auth/refresh');
  expect(refresh.status()).toBe(200);
  const auth = await refresh.json(); const headers = { Authorization: 'Bearer ' + auth.token };
  expect((await page.request.get(acceptanceApi + '/api/v1/cart', { headers })).status()).toBe(200);
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities', { headers })).status()).toBe(403);
  await page.reload(); await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
});

test('invalid MFA and a network failure retain the pending step and permit an explicit successful retry', async ({ page, fixture }) => {
  await credentials(page, staff, fixture.adminEmail, fixture.password);
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  await page.getByLabel('Authentication code', { exact: true }).fill(code === '000000' ? '111111' : '000000');
  await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText("That code didn't work");
  await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/refresh')).status()).toBe(401);
  await page.route('**/api/v1/auth/sign-in/verify', route => route.abort('internetdisconnected'));
  await page.getByLabel('Authentication code', { exact: true }).fill(code);
  await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click();
  await expect(page.getByRole('alert')).toBeVisible();
  await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
  await page.unroute('**/api/v1/auth/sign-in/verify');
  await completeMfa(page);
});

test('revoked management session requires new credentials and MFA', async ({ page, fixture }) => {
  await credentials(page, staff, fixture.adminEmail, fixture.password); await completeMfa(page);
  const response = await page.request.post(acceptanceApi + '/__fixture/session', { data: { email: fixture.adminEmail, management: true, action: 'revoke' } });
  expect(response.ok()).toBe(true);
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
});
