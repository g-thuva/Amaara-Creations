import { test, expect, acceptanceApi } from '../fixtures';

test('six-character passwords require uppercase, a number and a symbol across new-password forms', async ({ page }) => {
  for (const route of ['register', 'reset-password']) {
    await page.goto('./' + route);
    const password = page.getByLabel(route === 'register' ? 'Password' : 'New Password', { exact: true });
    await expect(password).toHaveAttribute('minlength', '6');
    for (const value of ['R7!ab', 'r7!abc', 'Rz!abc', 'R71abc', 'R71 ab']) {
      await password.fill(value);
      expect(await password.evaluate((input: HTMLInputElement) => input.validity.valid), value).toBe(false);
    }
    await password.fill('R7!abc');
    expect(await password.evaluate((input: HTMLInputElement) => input.validity.valid)).toBe(true);
    await expect(page.getByText(/Use 6–128 characters, including/)).toBeVisible();
  }
});

test('a customer can register and change a six-character password through the real workflow', async ({ page }) => {
  const email = `password-policy-${Date.now()}@example.test`;
  await page.goto('./register');
  await page.getByLabel('Full Name', { exact: true }).fill('Synthetic password policy test');
  await page.getByLabel('Email Address', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill('R7!abc');
  await page.getByLabel('Confirm Password', { exact: true }).fill('R7!abc');
  await page.getByRole('button', { name: 'Create Account', exact: true }).click();
  await expect(page).toHaveURL(/\/login$/);
  const confirmation = await page.request.post(acceptanceApi + '/__fixture/confirmation', { data: { email } });
  expect(confirmation.ok()).toBe(true);
  const { url } = await confirmation.json();
  await page.goto('./' + new URL(url).hash);
  await expect(page.getByRole('heading', { name: 'Email Verified', exact: true })).toBeVisible();
  await page.goto('./login');
  await page.getByLabel('Email Address', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill('R7!abc');
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page).toHaveURL(/\/profile$/);
  await page.goto('./account/security');
  await page.getByLabel('Current password', { exact: true }).fill('R7!abc');
  await page.getByLabel('New password', { exact: true }).fill('T9@abc');
  await page.getByLabel('Confirm new password', { exact: true }).fill('T9@abc');
  await page.getByRole('button', { name: 'Change password', exact: true }).click();
  await expect(page).toHaveURL(/\/login$/);
  await page.getByLabel('Email Address', { exact: true }).fill(email);
  await page.getByLabel('Password', { exact: true }).fill('T9@abc');
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page).toHaveURL(/\/profile$/);
});
