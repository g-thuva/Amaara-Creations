import { test, expect } from '../fixtures';

test('anonymous storefront requires credentials and exposes no runtime simulator', async ({ page }) => {
  const simulatorRequests: string[] = [];
  page.on('request', request => { if (request.url().includes('/simulator/')) simulatorRequests.push(request.url()); });
  await page.goto('./login');
  await expect(page.getByLabel('Email Address', { exact: true })).toBeVisible();
  await expect(page.getByLabel('Password', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Continue as test customer' })).toHaveCount(0);
  await expect(page.getByRole('link', { name: /administration|dashboard/i })).toHaveCount(0);
  await page.reload();
  await expect(page.getByLabel('Email Address', { exact: true })).toBeVisible();
  await page.goto('./profile');
  await expect(page).toHaveURL(/\/login$/);
  expect(simulatorRequests).toEqual([]);
});
