import { test, expect } from '../fixtures';
import type { Page } from '@playwright/test';

async function signIn(page: Page, fixture: { customerEmail: string; password: string }) {
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: /^(Sign In|Verify and sign in)$/ }).click();
  await expect(page).toHaveURL(/\/profile$/);
}

test('sign-out finishes cookie revocation before returning home and protecting direct account entry', async ({ page, fixture }) => {
  await signIn(page, fixture);
  let release!: () => void;
  let received = false;
  const gate = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/v1/auth/logout', async route => { received = true; await gate; await route.continue(); });
  await page.getByRole('button', { name: 'Sign out', exact: true }).click();
  try {
    await expect.poll(() => received).toBe(true);
    await expect(page).toHaveURL(/\/profile$/);
  } finally { release(); }
  await expect(page).toHaveURL(/\/Amaara-Creations\/?$/);
  await page.goto('./profile');
  await expect(page).toHaveURL(/\/login$/);
});

test('all historical account aliases resolve to the same protected pages after reload', async ({ page, fixture }) => {
  await signIn(page, fixture);
  for (const [alias, canonical, title] of [['account', 'profile', 'Your profile'], ['account/profile', 'profile', 'Your profile'], ['account/addresses', 'addresses', 'Address book'], ['account/orders', 'orders', 'Your orders']]) {
    await page.goto(`./${alias}`);
    await expect(page).toHaveURL(new RegExp(`/${canonical}$`));
    // The alias can change the URL while cookie-backed restoration is pending.
    await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible();
    await page.reload();
    await expect(page.getByRole('heading', { name: title, exact: true })).toBeVisible();
    await expect(page).toHaveURL(new RegExp(`/${canonical}$`));
  }
});

test('signing out every session keeps its confirmation and protects direct order entry', async ({ page, fixture }) => {
  await signIn(page, fixture);
  await page.goto('./account/security');
  await page.getByRole('button', { name: 'Sign out of all devices', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Confirm', exact: true }).click();
  await expect(page).toHaveURL(/\/login$/);
  await expect(page.getByText('Signed out of all devices.', { exact: true })).toBeVisible();
  await page.goto('./orders');
  await expect(page).toHaveURL(/\/login$/);
});
