import { test, expect, acceptanceApi } from '../fixtures';

test('abandoned first-time staff enrollment cannot reach operations and cancellation requires credentials again', async ({ page, request }) => {
  const fixture = await (await request.post(acceptanceApi + '/__fixture/operations')).json();
  const code = (await (await request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  const owner = await request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: fixture.superAdminEmail, password: fixture.basic.password, authenticatorCode: code } }); expect(owner.ok()).toBeTruthy();
  expect((await request.put(acceptanceApi + '/api/v1/admin/staff', { headers: { Authorization: 'Bearer ' + (await owner.json()).token }, data: { email: fixture.basic.customerEmail, roles: ['Admin'], reason: 'Disposable abandoned enrollment acceptance fixture' } })).ok()).toBeTruthy();
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.basic.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.basic.password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Secure your Amaara account', exact: true })).toBeVisible();
  await expect(page.getByLabel('Password', { exact: true })).toHaveCount(0);
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Secure your Amaara account', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Use a different account', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  expect((await page.request.get(acceptanceApi + '/api/v1/auth/sign-in')).status()).toBe(401);
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
});
