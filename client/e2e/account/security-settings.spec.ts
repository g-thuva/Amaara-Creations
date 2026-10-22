import { createHmac } from 'node:crypto';
import { test, expect, acceptanceApi } from '../fixtures';

function totp(secret: string) {
  let bits = 0; let value = 0; const bytes: number[] = [];
  for (const character of secret) {
    value = (value << 5) | 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'.indexOf(character); bits += 5;
    if (bits >= 8) { bits -= 8; bytes.push((value >> bits) & 255); }
  }
  const counter = Buffer.alloc(8); counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30000)));
  const hash = createHmac('sha1', Buffer.from(bytes)).update(counter).digest();
  return ((hash.readUInt32BE(hash[19] & 15) & 0x7fffffff) % 1000000).toString().padStart(6, '0');
}

test('customer enrolls an authenticator, saves one-time recovery and signs in with recovery', async ({ page, fixture }) => {
  const signedIn = await page.request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } });
  expect(signedIn.ok()).toBeTruthy();
  await page.goto('./account/security');
  await page.getByLabel('Confirm your password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Set up authenticator', exact: true }).click();
  const key = await page.locator('.s-setup-key').innerText();
  await page.getByLabel('Confirm your password', { exact: true }).fill(fixture.password);
  await page.getByLabel('Authenticator code', { exact: true }).fill(totp(key));
  await page.getByRole('button', { name: 'Verify and enable', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Save your recovery codes', exact: true })).toBeVisible();
  await expect(page.locator('.s-recovery-codes li')).toHaveCount(10);
  const recoveryCode = await page.locator('.s-recovery-codes code').first().innerText();
  await page.getByRole('button', { name: /I have saved these codes/ }).click();
  await expect(page).toHaveURL(/\/login$/);
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByText('Use a recovery code instead', { exact: true }).click();
  await page.getByLabel('Recovery code', { exact: true }).fill(recoveryCode);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page).toHaveURL(/\/profile$/);
  await page.goto('./account/security');
  await expect(page.getByText('Enabled · 9 recovery codes remaining', { exact: true })).toBeVisible();
  await page.setViewportSize({ width: 320, height: 900 });
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
});

test('a customer signs out another device without terminating the current device', async ({ page, browser, fixture }) => {
  const other = await browser.newContext();
  try {
    for (const request of [page.request, other.request]) {
      expect((await request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } })).ok()).toBeTruthy();
    }
    await page.goto('./account/security');
    await expect(page.locator('.s-session-list li')).toHaveCount(2);
    await page.getByRole('button', { name: 'Sign out device', exact: true }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Confirm', exact: true }).click();
    await expect(page.locator('.s-session-list li')).toHaveCount(1);
    expect((await other.request.post(`${acceptanceApi}/api/v1/auth/refresh`)).status()).toBe(401);
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Account security', exact: true })).toBeVisible();
  } finally { await other.close(); }
});
