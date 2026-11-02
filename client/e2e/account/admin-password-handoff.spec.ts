import { test, expect, acceptanceApi } from '../fixtures';
import { fillAuthenticator } from '../staffAuth';

const frontendPort = process.env.AMAARA_LOCAL_HANDOFF === '1' ? '5179' : '5178';
const adminPort = process.env.AMAARA_LOCAL_HANDOFF === '1' ? '5173' : '5179';

test('admin stays on the frontend for authentication and opens the styled dashboard only after a valid code', async ({ page, fixture }) => {
  let passwords = 0;
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  page.on('request', request => {
    if (request.method() === 'POST' && request.url().endsWith('/auth/sign-in')) passwords++;
  });
  await page.goto('./login');
  expect(new URL(page.url()).port).toBe(frontendPort);
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
  expect(new URL(page.url()).port).toBe(frontendPort);
  await expect(page.getByLabel('Password', { exact: true })).toHaveCount(0);
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toHaveCount(0);
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
  await page.reload();
  await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
  expect(new URL(page.url()).port).toBe(frontendPort);
  await fillAuthenticator(page);
  await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  expect(new URL(page.url()).port).toBe(adminPort);
  await expect(page.getByRole('navigation', { name: 'Administration', exact: true })).toBeVisible();
  expect(await page.locator('.admin-sidebar').evaluate(sidebar => getComputedStyle(sidebar).position)).toBe('fixed');
  expect(await page.locator('.admin-sidebar').evaluate(sidebar => sidebar.getBoundingClientRect().width)).toBeGreaterThan(200);
  await page.screenshot({ path: '../.runtime/admin-after-mfa-dashboard.png', fullPage: true });
  expect(passwords).toBe(1);
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  expect(errors).toEqual([]);
});

test('restoring a pending admin sign-in keeps verification on the frontend', async ({ page, fixture }) => {
  const response = await page.request.post(acceptanceApi + '/api/v1/auth/sign-in', { data: { email: fixture.adminEmail, password: fixture.password } });
  expect(response.status()).toBe(202);
  await page.goto('./login');
  await expect(page.getByLabel('Authentication code', { exact: true })).toBeVisible();
  expect(new URL(page.url()).port).toBe(frontendPort);
});

test('first-time admin QR setup and cancellation stay on the frontend', async ({ page, request }) => {
  const operations = await (await request.post(acceptanceApi + '/__fixture/operations')).json();
  const code = (await (await request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  const owner = await request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: operations.superAdminEmail, password: operations.basic.password, authenticatorCode: code } });
  expect(owner.ok()).toBeTruthy();
  const promote = await request.put(acceptanceApi + '/api/v1/admin/staff', { headers: { Authorization: 'Bearer ' + (await owner.json()).token }, data: { email: operations.basic.customerEmail, roles: ['Admin'], reason: 'Disposable admin password handoff regression' } });
  expect(promote.ok()).toBeTruthy();
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(operations.basic.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(operations.basic.password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Secure your Amaara account', exact: true })).toBeVisible();
  expect(new URL(page.url()).port).toBe(frontendPort);
  await expect(page.locator('.mfa-qr svg')).toBeVisible();
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Secure your Amaara account', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: 'Use a different account', exact: true }).click();
  await expect(page.getByLabel('Password', { exact: true })).toBeVisible();
  expect(new URL(page.url()).port).toBe(frontendPort);
  expect((await page.request.get(acceptanceApi + '/api/v1/auth/sign-in')).status()).toBe(401);
});

test('incorrect admin credentials stay on the frontend', async ({ page, fixture }) => {
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
  await page.getByLabel('Password', { exact: true }).fill('Deliberately-incorrect-test-password');
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Unable to sign in');
  expect(new URL(page.url()).port).toBe(frontendPort);
});

test('customer password still opens the customer profile on the frontend', async ({ page, fixture }) => {
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page).toHaveURL(/\/profile$/);
  expect(new URL(page.url()).port).toBe(frontendPort);
});
