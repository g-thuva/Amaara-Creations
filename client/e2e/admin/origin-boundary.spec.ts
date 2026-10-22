import { test, expect, acceptanceApi } from '../fixtures';

test('storefront JavaScript cannot exchange management cookies on the shared API host', async ({ page, fixture }) => {
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code } })).ok()).toBeTruthy();
  await page.goto('http://127.0.0.1:5178/Amaara-Creations/');
  const result = await page.evaluate(async api => {
    const response = await fetch(api + '/api/v1/auth/staff/refresh', { method: 'POST', credentials: 'include' });
    return { status: response.status, body: await response.text() };
  }, acceptanceApi);
  expect(result.status).toBe(403); expect(result.body).not.toContain('token');
  await page.goto('./login');
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
});
