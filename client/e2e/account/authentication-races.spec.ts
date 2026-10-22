import { test, expect, acceptanceApi } from '../fixtures';

test('a delayed anonymous restoration cannot clear a newer real sign-in or its cookie', async ({ page, fixture }) => {
  let release!: () => void; const gate = new Promise<void>(resolve => { release = resolve; });
  let restoring = false;
  await page.route('**/api/v1/auth/refresh', async route => {
    const response = await route.fetch(); restoring = true; await gate;
    try { await route.fulfill({ response }); } catch { /* Explicit sign-in aborted the older network request. */ }
  });
  await page.goto('./login');
  try {
    await expect.poll(() => restoring).toBe(true);
    await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
    await page.getByLabel('Password', { exact: true }).fill(fixture.password);
    await page.getByRole('button', { name: 'Sign In', exact: true }).click();
    await expect(page).toHaveURL(/\/profile$/);
  } finally { release(); }
  await page.unroute('**/api/v1/auth/refresh');
  await page.reload();
  await expect(page.getByRole('heading', { level: 1, name: 'Your profile', exact: true })).toBeVisible();
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/refresh')).ok()).toBeTruthy();
});

test('a storefront staff sign-in never redirects to or restores management', async ({ page, context, fixture }) => {
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code } })).ok()).toBeTruthy();
  await page.goto('./profile'); await expect(page.getByRole('heading', { level: 1, name: 'Your profile', exact: true })).toBeVisible();
  await expect(page.getByRole('link', { name: /admin|management|studio operations/i })).toHaveCount(0);
  const staff = await context.newPage(); await staff.goto('http://127.0.0.1:5179/Amaara-Creations/login');
  await expect(staff.getByRole('heading', { name: 'Staff sign in', exact: true })).toBeVisible();
  await expect(staff.getByRole('heading', { name: 'Dashboard', exact: true })).toHaveCount(0); await staff.close();
});
