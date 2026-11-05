import { test, expect, acceptanceApi, managementUrl } from '../fixtures';

test('an unresolved restoration waits before credential entry and preserves the subsequent real sign-in', async ({ page, fixture }) => {
  let release!: () => void; const gate = new Promise<void>(resolve => { release = resolve; });
  let restoring = false;
  await page.route('**/api/v1/auth/sign-in', async route => {
    const response = await route.fetch(); restoring = true; await gate;
    try { await route.fulfill({ response }); } catch { /* Explicit sign-in aborted the older network request. */ }
  });
  await page.goto('./login');
  try {
    await expect.poll(() => restoring).toBe(true);
    await expect(page.getByText('Checking your session...', { exact: true })).toBeVisible();
    await expect(page.getByLabel('Email Address', { exact: true })).toHaveCount(0);
  } finally { release(); }
  await page.unroute('**/api/v1/auth/sign-in');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page).toHaveURL(/\/profile$/);
  await page.reload();
  await expect(page.getByRole('heading', { level: 1, name: 'Your profile', exact: true })).toBeVisible();
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/refresh')).ok()).toBeTruthy();
});

test('customer and staff identities restore independently without overwriting each other', async ({ page, context, fixture }) => {
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  const login = await page.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.customerEmail, password: fixture.password } });
  expect(login.ok()).toBeTruthy();
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities', { headers: { Authorization: 'Bearer ' + (await login.json()).token } })).status()).toBe(403);
  await page.goto('./profile'); await expect(page.getByRole('heading', { level: 1, name: 'Your profile', exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: /admin|management|studio operations/i })).toHaveCount(0);
  const staff = await context.newPage(); await staff.goto(managementUrl + 'login');
  await expect(staff.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  expect((await staff.request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code } })).ok()).toBe(true);
  await staff.goto(managementUrl + 'login');
  await expect(staff.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  await staff.close();
});
