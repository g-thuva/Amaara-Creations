import { test, expect, acceptanceApi } from '../fixtures';
test('audit identifiers copy and safe metadata and full-ID tooltips work with the keyboard', async ({ page, context }) => {
  const fixture = await (await page.request.post(acceptanceApi + '/__fixture/operations')).json();
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: fixture.superAdminEmail, password: fixture.basic.password, authenticatorCode: code } })).ok()).toBe(true);
  await page.goto('./admin/audit');
  await context.grantPermissions(['clipboard-write'], { origin: new URL(page.url()).origin });
  const id = page.locator('.admin-copy-id').filter({ has: page.getByRole('button', { name: 'Copy actor identifier', exact: true }) }).first();
  await id.getByRole('button').click(); await expect(id.getByRole('status')).toHaveText('Copied');
  await id.locator('.admin-tooltip').focus(); await expect(id.getByRole('tooltip')).toBeVisible();
  await page.keyboard.press('Escape'); await expect(id.getByRole('tooltip')).not.toBeVisible();
  const metadata = page.locator('details.admin-disclosure').first(); await metadata.locator('summary').focus(); await page.keyboard.press('Enter');
  await expect(metadata.getByText('Before', { exact: true })).toBeVisible(); await expect(metadata.getByText('After', { exact: true })).toBeVisible();
});
