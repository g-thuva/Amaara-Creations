import { createHmac } from 'node:crypto';
import jsQR from 'jsqr';
import AxeBuilder from '@axe-core/playwright';
import type { Page } from '@playwright/test';
import { test, expect, acceptanceApi } from '../fixtures';
import { fillAuthenticator } from '../staffAuth';

async function accessibleResponsiveStage(page: Page) {
  for (const width of [360, 390, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 1000 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  }
  const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
  expect(result.violations).toEqual([]);
}

function totp(secret: string, offset = 0) {
  let bits = 0; let value = 0; const bytes: number[] = [];
  for (const character of secret) { value = (value << 5) | 'ABCDEFGHIJKLMNOPQRSTUVWXYZ234567'.indexOf(character); bits += 5; if (bits >= 8) { bits -= 8; bytes.push((value >> bits) & 255); } }
  const counter = Buffer.alloc(8); counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30000) + offset));
  const hash = createHmac('sha1', Buffer.from(bytes)).update(counter).digest();
  return ((hash.readUInt32BE(hash[19] & 15) & 0x7fffffff) % 1000000).toString().padStart(6, '0');
}
async function qrSecret(page: Page, email: string) {
  const image = await page.locator('.mfa-qr svg').evaluate(async svg => {
    const url = URL.createObjectURL(new Blob([new XMLSerializer().serializeToString(svg)], { type: 'image/svg+xml' }));
    const image = new Image(); image.src = url; await image.decode();
    const canvas = document.createElement('canvas'); canvas.width = image.width; canvas.height = image.height;
    const context = canvas.getContext('2d')!; context.drawImage(image, 0, 0);
    const data = context.getImageData(0, 0, canvas.width, canvas.height); URL.revokeObjectURL(url);
    return { data: Array.from(data.data), width: data.width, height: data.height };
  });
  const decoded = jsQR(new Uint8ClampedArray(image.data), image.width, image.height);
  expect(Boolean(decoded), 'The actual QR pixels must decode locally').toBe(true);
  const uri = new URL(decoded!.data);
  expect(uri.protocol).toBe('otpauth:'); expect(uri.hostname).toBe('totp');
  expect(decodeURIComponent(uri.pathname.slice(1))).toBe('Amaara Creations:' + email);
  expect(uri.searchParams.get('issuer')).toBe('Amaara Creations');
  return uri.searchParams.get('secret')!;
}
async function credentials(page: Page, email: string, password: string) {
  await page.goto('http://127.0.0.1:5178/Amaara-Creations/login');
  await page.getByLabel('Email Address', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill(password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
}
async function dashboard(page: Page) {
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  await expect(page.getByRole('heading', { level: 1, name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.getByText('Total Orders', { exact: true })).toBeVisible();
}
async function acknowledge(page: Page) {
  await expect(page.getByRole('heading', { name: 'Save your recovery codes', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Continue to my account', exact: true })).toBeDisabled();
  await page.getByLabel('I have saved my recovery codes somewhere safe.', { exact: true }).check();
  await page.getByRole('button', { name: 'Continue to my account', exact: true }).click();
  await dashboard(page);
}

test('customers see only password sign-in while staff reveal verification only after the password succeeds', async ({ page, fixture }) => {
  await page.goto('./login');
  await expect(page.getByLabel('Authentication code', { exact: true })).toHaveCount(0);
  await expect(page.getByRole('img', { name: 'Authenticator enrollment QR code' })).toHaveCount(0);
  await accessibleResponsiveStage(page);
  await page.getByLabel('Email Address', { exact: true }).focus();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('link', { name: 'Forgot password?', exact: true })).toBeFocused();
  await page.keyboard.press('Tab');
  await expect(page.getByLabel('Password', { exact: true })).toBeFocused();
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Show password', exact: true }).click();
  await expect(page.getByLabel('Password', { exact: true })).toHaveAttribute('type', 'text');
  await page.getByRole('button', { name: 'Hide password', exact: true }).click();
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page).toHaveURL(/\/profile$/);
  expect((await page.context().cookies()).some(cookie => cookie.name === 'amaara_refresh_staff')).toBe(false);
  await page.reload(); await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
});

test('returning staff verify on the shared screen without a pending JWT, repeat password or QR setup', async ({ page, fixture }) => {
  await credentials(page, fixture.adminEmail, fixture.password);
  await expect(page.getByRole('heading', { name: /Verify it/ })).toBeVisible();
  await expect(page.getByLabel('Password', { exact: true })).toHaveCount(0);
  await expect(page.locator('.mfa-qr')).toHaveCount(0);
  await accessibleResponsiveStage(page);
  await page.getByLabel('Authentication code', { exact: true }).focus();
  await page.keyboard.press('Tab');
  await expect(page.getByRole('button', { name: 'Verify and sign in', exact: true })).toBeFocused();
  expect((await page.request.get(acceptanceApi + '/api/v1/auth/me')).status()).toBe(401);
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
  await page.getByLabel('Authentication code', { exact: true }).fill('123456');
  // The code cannot be assumed wrong at this instant; submit a guaranteed
  // invalid value directly to verify the backend while preserving the UI state.
  expect((await page.request.post(acceptanceApi + '/api/v1/auth/sign-in/verify', { data: { code: 'invalid' } })).status()).toBe(400);
  await fillAuthenticator(page);
  await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click();
  await dashboard(page); await page.reload(); await dashboard(page);
});

test('new staff scan one stable QR, save recovery and reach operations; lost-phone recovery securely replaces the authenticator', async ({ page, request }) => {
  test.setTimeout(180000);
  const operations = await (await request.post(acceptanceApi + '/__fixture/operations')).json();
  const ownerCode = (await (await request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  const owner = await request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: operations.superAdminEmail, password: operations.basic.password, authenticatorCode: ownerCode } }); expect(owner.ok()).toBe(true);
  expect((await request.put(acceptanceApi + '/api/v1/admin/staff', { headers: { Authorization: 'Bearer ' + (await owner.json()).token }, data: { email: operations.basic.customerEmail, roles: ['Admin'], reason: 'Disposable guided MFA browser acceptance' } })).ok()).toBe(true);
  const email = operations.basic.customerEmail; const password = operations.basic.password;
  await page.addInitScript(() => {
    Object.defineProperty(navigator, 'clipboard', { configurable: true, value: { writeText: async (value: string) => { (window as unknown as { copied: string }).copied = value; } } });
    window.print = () => { (window as unknown as { printed: boolean }).printed = true; };
  });
  await credentials(page, email, password);
  await expect(page.getByRole('heading', { name: 'Secure your Amaara account', exact: true })).toBeVisible();
  const secret = await qrSecret(page, email);
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Secure your Amaara account', exact: true })).toBeVisible();
  expect((await qrSecret(page, email)) === secret).toBe(true);
  await page.getByText('Can’t scan? Enter the setup key manually.', { exact: true }).click();
  expect((await page.locator('.s-setup-key').textContent()) === secret).toBe(true);
  await page.getByRole('button', { name: 'Copy setup key', exact: true }).click();
  expect(await page.evaluate(secret => (window as unknown as { copied: string }).copied === secret, secret)).toBe(true);
  await page.getByText('Can’t scan? Enter the setup key manually.', { exact: true }).click();
  for (const width of [360, 390, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 1000 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
    await page.evaluate(() => window.scrollTo(0, 0));
    await page.screenshot({ path: `../.runtime/guided-mfa/enrollment-${width}-${process.env.AMAARA_E2E_PRODUCTION === '1' ? 'production' : 'development'}.png`, fullPage: true, mask: [page.locator('.mfa-qr'), page.locator('.s-setup-key:visible')] });
  }
  await accessibleResponsiveStage(page);
  const accepted = new Set([-3, -2, -1, 0, 1, 2, 3].map(offset => totp(secret, offset)));
  let wrong = '000000'; while (accepted.has(wrong)) wrong = (Number(wrong) + 1).toString().padStart(6, '0');
  await page.getByLabel('Authentication code', { exact: true }).fill(wrong);
  await page.getByRole('button', { name: 'Verify and continue', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText("That code didn't work");
  expect((await qrSecret(page, email)) === secret).toBe(true);
  const confirmation = totp(secret);
  await page.getByLabel('Authentication code', { exact: true }).evaluate((input, code) => {
    const data = new DataTransfer(); data.setData('text/plain', code.slice(0, 3) + ' ' + code.slice(3));
    input.dispatchEvent(new ClipboardEvent('paste', { bubbles: true, clipboardData: data }));
  }, confirmation);
  await expect(page.getByLabel('Authentication code', { exact: true })).toHaveValue(confirmation);
  await page.getByRole('button', { name: 'Verify and continue', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Save your recovery codes', exact: true })).toBeVisible();
  const recoveryCode = (await page.locator('.s-recovery-codes code').first().textContent())!;
  expect((await page.request.get(acceptanceApi + '/api/v1/admin/capabilities')).status()).toBe(401);
  const downloaded = page.waitForEvent('download'); await page.getByRole('button', { name: 'Download codes', exact: true }).click();
  expect((await downloaded).suggestedFilename()).toBe('amaara-recovery-codes.txt');
  await page.getByRole('button', { name: 'Copy codes', exact: true }).click();
  expect(await page.evaluate(() => (window as unknown as { copied: string }).copied.split('\n').filter(line => /^[A-F0-9]{24}$/.test(line)).length)).toBe(10);
  await page.getByRole('button', { name: 'Print codes', exact: true }).click();
  expect(await page.evaluate(() => (window as unknown as { printed: boolean }).printed)).toBe(true);
  for (const width of [360, 390, 768, 1024, 1440]) {
    await page.setViewportSize({ width, height: 1000 });
    expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  }
  await accessibleResponsiveStage(page);
  await acknowledge(page);
  await expect.poll(() => totp(secret), { timeout: 35000 }).not.toBe(confirmation);
  await credentials(page, email, password);
  await expect(page.getByRole('heading', { name: /Verify it/ })).toBeVisible();
  await expect(page.locator('.mfa-qr')).toHaveCount(0);
  await page.getByLabel('Authentication code', { exact: true }).fill(totp(secret));
  await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click(); await dashboard(page);
  await credentials(page, email, password);
  await page.getByRole('button', { name: 'Use a recovery code', exact: true }).click();
  await page.getByLabel('Recovery code', { exact: true }).fill(recoveryCode);
  await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Secure your Amaara account', exact: true })).toBeVisible();
  const replacement = await qrSecret(page, email); expect(replacement === secret).toBe(false);
  await page.getByLabel('Authentication code', { exact: true }).fill(totp(replacement));
  await page.getByRole('button', { name: 'Verify and continue', exact: true }).click();
  await acknowledge(page); await page.reload(); await dashboard(page);
  expect(await page.evaluate(() => Object.keys(localStorage).filter(key => /token|secret|mfa|role/i.test(key)))).toEqual([]);
});
