import AxeBuilder from '@axe-core/playwright';
import { test, expect, acceptanceApi, type Page } from '../fixtures';

async function setup(page: Page, support = false) {
  const response = await page.request.post(acceptanceApi + '/__fixture/operations');
  expect(response.ok()).toBeTruthy();
  const fixture = await response.json();
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  const login = await page.request.post(acceptanceApi + '/api/v1/auth/staff/login', {
    data: { email: support ? fixture.supportEmail : fixture.basic.adminEmail, password: fixture.basic.password, authenticatorCode: code },
  });
  expect(login.ok()).toBeTruthy();
  await page.goto('./admin/customers');
  await page.getByLabel('Search name or email', { exact: true }).fill(fixture.basic.customerEmail);
  return { fixture, headers: { Authorization: 'Bearer ' + (await login.json()).token } };
}

async function openAction(page: Page, email: string, action: string) {
  const row = page.getByRole('row').filter({ hasText: email });
  await row.getByRole('button', { name: 'Manage customer ' + email, exact: true }).click();
  await row.getByRole('button', { name: action, exact: true }).click();
  const dialog = page.getByRole('dialog', { name: action, exact: true });
  await expect(dialog.getByLabel('Reason', { exact: true })).toBeFocused();
  return dialog;
}

for (const width of [1440, 390]) {
  test(`admin manages customer access from the list at ${width}px and removal preserves history`, async ({ page, browser }) => {
    await page.setViewportSize({ width, height: 900 });
    const { fixture } = await setup(page);
    const customerContext = await browser.newContext();
    try {
      const customer = await customerContext.newPage();
      const login = await customer.request.post(acceptanceApi + '/api/v1/auth/login', {
        data: { email: fixture.basic.customerEmail, password: fixture.basic.password },
      });
      expect(login.ok()).toBeTruthy();
      const headers = { Authorization: 'Bearer ' + (await login.json()).token };
      const row = page.getByRole('row').filter({ hasText: fixture.basic.customerEmail });
      for (const [action, state] of [['Suspend customer', 'Suspended'], ['Reactivate customer', 'Active'], ['Block customer', 'Blocked'], ['Reactivate customer', 'Active']]) {
        const dialog = await openAction(page, fixture.basic.customerEmail, action);
        await expect(dialog.getByRole('button', { name: action, exact: true })).toBeDisabled();
        await dialog.getByLabel('Reason', { exact: true }).fill('Administrator customer access verification');
        await dialog.getByRole('button', { name: action, exact: true }).click();
        await expect(dialog).not.toBeVisible();
        await expect(row).toContainText(state);
        expect((await customer.request.get(acceptanceApi + '/api/v1/orders', { headers })).status()).toBe(401);
        if (state !== 'Active') {
          expect((await customer.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.basic.customerEmail, password: fixture.basic.password } })).status()).toBe(401);
        }
      }
      const dialog = await openAction(page, fixture.basic.customerEmail, 'Remove customer');
      await dialog.getByLabel('Reason', { exact: true }).fill('Remove a disposable customer and preserve business records');
      const submit = dialog.getByRole('button', { name: 'Remove customer', exact: true });
      await expect(submit).toBeDisabled();
      await dialog.getByRole('checkbox').check();
      await expect(submit).toBeEnabled();
      expect(await dialog.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
      const audit = await new AxeBuilder({ page }).include('dialog[open]').withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
      expect(audit.violations).toEqual([]);
      await page.screenshot({ path: `../.runtime/customer-access-${width}-removal-dialog.png` });
      await submit.click();
      await expect(dialog).not.toBeVisible();
      await expect(row).toHaveCount(0);
      await expect(page.getByRole('status').filter({ hasText: 'Customer removed.' })).toBeVisible();
      expect((await customer.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.basic.customerEmail, password: fixture.basic.password } })).status()).toBe(401);
      await page.reload();
      await page.getByLabel('Search name or email', { exact: true }).fill(fixture.basic.customerEmail);
      await expect(row).toHaveCount(0);
      await page.getByLabel('Account status', { exact: true }).selectOption('Removed');
      await expect(row).toContainText('Removed');
      await page.screenshot({ path: `../.runtime/customer-access-${width}-removed-list.png` });
      await row.getByRole('link').click();
      await expect(page.getByRole('table', { name: 'Recent customer orders (up to 50)', exact: true })).toContainText('REFUND-FIXTURE-');
      await expect(page.getByRole('table', { name: 'Recent account access history (up to 30)', exact: true })).toContainText('Remove a disposable customer');
      await expect(page.locator('aside[aria-label="Customer summary"]')).toContainText('Removed');
      await page.goto('./admin/customers');
      await page.getByLabel('Search name or email', { exact: true }).fill(fixture.basic.customerEmail);
      await page.getByLabel('Account status', { exact: true }).selectOption('Removed');
      const restore = await openAction(page, fixture.basic.customerEmail, 'Reactivate customer');
      await restore.getByLabel('Reason', { exact: true }).fill('Restore the disposable account after review');
      await restore.getByRole('button', { name: 'Reactivate customer', exact: true }).click();
      await expect(restore).not.toBeVisible();
      await page.getByLabel('Account status', { exact: true }).selectOption('');
      await expect(row).toContainText('Active');
      expect((await customer.request.get(acceptanceApi + '/api/v1/orders', { headers })).status()).toBe(401);
      expect((await customer.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.basic.customerEmail, password: fixture.basic.password } })).ok()).toBeTruthy();
    } finally { await customerContext.close(); }
  });
}

test('a stale removal cannot overwrite a newer decision and reloading keeps the draft', async ({ page }) => {
  const { fixture, headers } = await setup(page);
  const dialog = await openAction(page, fixture.basic.customerEmail, 'Remove customer');
  const reason = 'Keep this removal draft while reviewing a concurrent account change';
  await dialog.getByLabel('Reason', { exact: true }).fill(reason);
  await dialog.getByRole('checkbox').check();
  const current = await (await page.request.get(`${acceptanceApi}/api/v1/admin/customers/${fixture.customerId}/operations`, { headers })).json();
  expect((await page.request.put(`${acceptanceApi}/api/v1/admin/customers/${fixture.customerId}/status`, {
    headers, data: { status: 'Blocked', reason: 'Another administrator blocked this account', version: current.customer.version },
  })).status()).toBe(204);
  await dialog.getByRole('button', { name: 'Remove customer', exact: true }).click();
  await expect(dialog.getByRole('alert')).toBeVisible();
  await expect(dialog.getByRole('button', { name: 'Remove customer', exact: true })).toBeDisabled();
  await dialog.getByRole('button', { name: 'Reload current status', exact: true }).click();
  await expect(dialog).toContainText('Blocked');
  await expect(dialog.getByLabel('Reason', { exact: true })).toHaveValue(reason);
  await expect(dialog.getByRole('checkbox')).toBeChecked();
  await dialog.getByRole('button', { name: 'Remove customer', exact: true }).click();
  await expect(dialog).not.toBeVisible();
  await page.getByLabel('Account status', { exact: true }).selectOption('Removed');
  await expect(page.getByRole('row').filter({ hasText: fixture.basic.customerEmail })).toContainText('Removed');
});

test('failed actions retain the form and cancellation leaves access unchanged', async ({ page }) => {
  const { fixture } = await setup(page);
  let dialog = await openAction(page, fixture.basic.customerEmail, 'Block customer');
  await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
  await expect(dialog).not.toBeVisible();
  const row = page.getByRole('row').filter({ hasText: fixture.basic.customerEmail });
  await expect(row.getByRole('button', { name: 'Manage customer ' + fixture.basic.customerEmail, exact: true })).toBeFocused();
  await expect(row).toContainText('Active');
  dialog = await openAction(page, fixture.basic.customerEmail, 'Block customer');
  await dialog.getByLabel('Reason', { exact: true }).fill('Preserve this explanation after a failed request');
  await page.route('**/api/v1/admin/customers/*/status', route => route.fulfill({ status: 503, contentType: 'application/json', body: '{}' }));
  await dialog.getByRole('button', { name: 'Block customer', exact: true }).click();
  await expect(dialog.getByRole('alert')).toBeVisible();
  await expect(dialog.getByLabel('Reason', { exact: true })).toHaveValue('Preserve this explanation after a failed request');
  await page.unroute('**/api/v1/admin/customers/*/status');
  await dialog.getByRole('button', { name: 'Block customer', exact: true }).click();
  await expect(dialog).not.toBeVisible();
  await expect(row).toContainText('Blocked');
});

test('support can inspect customers but cannot use access actions', async ({ page }) => {
  const { fixture } = await setup(page, true);
  await expect(page.getByRole('row').filter({ hasText: fixture.basic.customerEmail })).toBeVisible();
  await expect(page.getByRole('columnheader', { name: 'Account actions', exact: true })).toHaveCount(0);
  await page.getByRole('row').filter({ hasText: fixture.basic.customerEmail }).getByRole('link').click();
  await expect(page.getByRole('table', { name: 'Recent customer orders (up to 50)', exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: /^(Block|Suspend|Remove|Reactivate) customer$/ })).toHaveCount(0);
});

test('removing or restoring the last customer on a filtered page returns to a populated page', async ({ page }) => {
  const { fixture, headers } = await setup(page);
  const prefix = 'Customer pagination ' + Date.now();
  for (let index = 0; index < 21; index++) {
    const registration = await page.request.post(acceptanceApi + '/api/v1/auth/register', {
      data: { name: `${prefix} ${index}`, email: `pagination-${Date.now()}-${index}@example.test`, password: fixture.basic.password, confirmPassword: fixture.basic.password },
    });
    expect(registration.ok(), await registration.text()).toBeTruthy();
  }
  await page.getByLabel('Search name or email', { exact: true }).fill(prefix);
  await expect(page.getByRole('navigation', { name: 'Results pages' })).toContainText('Page 1 of 2');
  await page.getByRole('button', { name: 'Next', exact: true }).click();
  const row = page.getByRole('table', { name: 'Registered customers', exact: true }).locator('tbody tr');
  await expect(row).toHaveCount(1);
  const email = (await row.locator('td').nth(1).textContent())!;
  const remove = await openAction(page, email, 'Remove customer');
  await remove.getByLabel('Reason', { exact: true }).fill('Remove the last entry on a disposable customer page');
  await remove.getByRole('checkbox').check();
  await remove.getByRole('button', { name: 'Remove customer', exact: true }).click();
  await expect(remove).not.toBeVisible();
  await expect(page.getByRole('navigation', { name: 'Results pages' })).toContainText('Page 1 of 1');
  await expect(row).toHaveCount(20);
  const current = await (await page.request.get(`${acceptanceApi}/api/v1/admin/customers?search=${encodeURIComponent(prefix)}&pageSize=100`, { headers })).json();
  for (const customer of current.customers) {
    expect((await page.request.put(`${acceptanceApi}/api/v1/admin/customers/${customer.id}/status`, {
      headers, data: { status: 'Removed', reason: 'Prepare a disposable filtered page for restoration', version: customer.version, acknowledged: true },
    })).status()).toBe(204);
  }
  await page.getByLabel('Account status', { exact: true }).selectOption('Removed');
  await expect(page.getByRole('navigation', { name: 'Results pages' })).toContainText('Page 1 of 2');
  await page.getByRole('button', { name: 'Next', exact: true }).click();
  await expect(row).toHaveCount(1);
  const removedEmail = (await row.locator('td').nth(1).textContent())!;
  const restore = await openAction(page, removedEmail, 'Reactivate customer');
  await restore.getByLabel('Reason', { exact: true }).fill('Restore the last entry on a disposable removed-account page');
  await restore.getByRole('button', { name: 'Reactivate customer', exact: true }).click();
  await expect(restore).not.toBeVisible();
  await expect(page.getByRole('navigation', { name: 'Results pages' })).toContainText('Page 1 of 1');
  await expect(row).toHaveCount(20);
});
