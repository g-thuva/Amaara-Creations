import { test, expect } from '../fixtures';
import { fillAuthenticator } from '../staffAuth';
  test('exposes the builder and production admin screens to an authorised administrator', async ({ page, fixture }) => {
    await page.goto('./login');
    await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
    await page.getByLabel('Password', { exact: true }).fill(fixture.password);
    await fillAuthenticator(page);
    await page.getByRole('button', { name: 'Sign In', exact: true }).click();
    await expect(page).not.toHaveURL(/\/login$/);
    await page.goto('./admin/custom-builder');
    await expect(page.getByRole('heading', { name: 'Custom builder configuration' })).toBeVisible();
    await expect(page.getByRole('button', { name: 'Test quote' })).toBeEnabled();
    await expect(page.getByText('Version history')).toBeVisible();

    await page.goto('./admin/custom-designs');
    await expect(page.getByRole('heading', { name: 'Custom design production' })).toBeVisible();
    await expect(page.getByLabel('Search order or customer')).toBeEditable();
    await expect(page.getByLabel('Proof state')).toBeEnabled();
    await expect(page.getByLabel('Production state')).toBeEnabled();
  });
