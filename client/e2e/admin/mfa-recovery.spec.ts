import { test, expect, acceptanceApi } from '../fixtures';
import { fillAuthenticator } from '../staffAuth';

test('a freshly verified owner approves employee recovery through confirmation and forces replacement enrollment', async ({ page, request }) => {
  const fixture = await (await request.post(acceptanceApi + '/__fixture/operations')).json();
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.superAdminEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.basic.password);
  await fillAuthenticator(page);
  await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click();
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await page.goto('http://127.0.0.1:5179/Amaara-Creations/admin/staff');
  await page.getByLabel('Recovery account email', { exact: true }).fill(fixture.basic.adminEmail);
  await page.getByLabel('Verified recovery reason', { exact: true }).fill('Disposable employee identity verified through independent test procedure.');
  await page.getByRole('button', { name: 'Review authenticator recovery', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'Approve authenticator recovery' })).toBeVisible();
  const approval = page.waitForResponse(response => response.url().endsWith('/admin/staff/mfa-recovery') && response.request().method() === 'POST');
  await page.getByRole('dialog').getByRole('button', { name: 'Confirm', exact: true }).click();
  expect((await approval).status()).toBe(200);
  await expect(page.getByRole('status').filter({ hasText: 'Changes saved.' })).toBeVisible();
  const pending = await page.request.post(acceptanceApi + '/api/v1/auth/sign-in', { data: { email: fixture.basic.adminEmail, password: fixture.basic.password } });
  expect(pending.status()).toBe(202);
  const body = await pending.json();
  expect(body.status).toBe('enroll'); expect(body.token).toBeUndefined();
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
});
