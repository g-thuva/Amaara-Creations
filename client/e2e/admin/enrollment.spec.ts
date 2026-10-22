import { createHmac } from 'node:crypto';
import { test, expect, acceptanceApi } from '../fixtures';

function authenticatorCode(secret: string) {
  let bits = 0; let value = 0; const bytes: number[] = [];
  for (const character of secret) {
    value = (value << 5) | 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'.indexOf(character); bits += 5;
    if (bits >= 8) { bits -= 8; bytes.push((value >> bits) & 255); }
  }
  const counter = Buffer.alloc(8); counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30000)));
  const hash = createHmac('sha1', Buffer.from(bytes)).update(counter).digest();
  return ((hash.readUInt32BE(hash[19] & 15) & 0x7fffffff) % 1000000).toString().padStart(6, '0');
}

test('a legitimately provisioned staff identity completes enrollment then reaches the dashboard with fresh MFA', async ({ page, request }) => {
  test.setTimeout(90000);
  const created = await request.post(acceptanceApi + '/__fixture/operations'); expect(created.ok()).toBeTruthy(); const fixture = await created.json();
  const code = (await (await request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  const owner = await request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: fixture.superAdminEmail, password: fixture.basic.password, authenticatorCode: code } }); expect(owner.ok()).toBeTruthy();
  const assigned = await request.put(acceptanceApi + '/api/v1/admin/staff', { headers: { Authorization: 'Bearer ' + (await owner.json()).token }, data: { email: fixture.basic.customerEmail, roles: ['Admin'], reason: 'Disposable MFA enrollment acceptance fixture' } }); expect(assigned.ok()).toBeTruthy();
  await request.post(acceptanceApi + '/api/v1/auth/staff/logout');
  await page.goto('./login'); await page.getByLabel('Email Address', { exact: true }).fill(fixture.basic.customerEmail); await page.getByLabel('Password', { exact: true }).fill(fixture.basic.password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Set up management security', exact: true })).toBeVisible();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toHaveCount(0);
  await page.getByLabel('Confirm your password', { exact: true }).fill(fixture.basic.password);
  await page.getByRole('button', { name: 'Set up authenticator', exact: true }).click();
  const secret = (await page.locator('.s-setup-key').textContent())!;
  await page.locator('.s-setup-key').evaluate(node => { node.textContent = '[disposable setup key hidden from test artifacts]'; });
  const confirmation = authenticatorCode(secret);
  await page.getByLabel('Confirm your password', { exact: true }).fill(fixture.basic.password);
  await page.getByLabel('Authenticator code', { exact: true }).fill(confirmation);
  await page.getByRole('button', { name: 'Verify and enable', exact: true }).click();
  await page.locator('.s-recovery-codes code').first().waitFor();
  await page.locator('.s-recovery-codes code').evaluateAll(nodes => nodes.forEach(node => { node.textContent = '[disposable recovery code hidden from test artifacts]'; }));
  await expect(page.getByRole('heading', { name: 'Save your recovery codes', exact: true })).toBeVisible();
  await expect(page.locator('.s-recovery-codes li')).toHaveCount(10);
  await page.getByRole('button', { name: /I have saved these codes/ }).click();
  await expect(page.getByRole('heading', { name: 'Staff sign in', exact: true })).toBeVisible();
  await expect.poll(() => authenticatorCode(secret), { timeout: 35000 }).not.toBe(confirmation);
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.basic.customerEmail); await page.getByLabel('Password', { exact: true }).fill(fixture.basic.password);
  await page.getByLabel('Authenticator code', { exact: true }).fill(authenticatorCode(secret));
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.getByText('Total Orders', { exact: true })).toBeVisible();
  await page.reload(); await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
});
