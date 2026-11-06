import { test, expect, acceptanceApi, type Page } from '../fixtures';
import AxeBuilder from '@axe-core/playwright';

for (const width of [1440, 768, 390]) {
  test('shipping zone and method dialogs fit ' + width + 'px with spaced controls and visible actions', async ({ page }) => {
    await operations(page);
    await page.setViewportSize({ width, height: 900 });
    await page.goto('./admin/shipping');
    await expect(page.getByRole('heading', { name: 'Shipping zones and methods', exact: true })).toBeVisible();
    await page.reload();
    await expect(page.getByRole('heading', { name: 'Shipping zones and methods', exact: true })).toBeVisible();
    await page.screenshot({ path: `../.runtime/shipping-reload-${width}.png` });
    for (const type of ['zone', 'method']) {
      await page.getByRole('button', { name: 'New ' + type, exact: true }).click();
      const dialog = page.getByRole('dialog', { name: 'New delivery ' + type, exact: true });
      await expect(dialog).toBeVisible();
      await expect(dialog.getByRole('button', { name: type === 'zone' ? 'Save zone' : 'Save delivery method', exact: true })).toBeInViewport();
      expect(await dialog.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
      for (const control of await dialog.locator('.admin-check').all()) {
        await control.scrollIntoViewIfNeeded();
        const checkbox = await control.locator('input').boundingBox();
        const text = await control.locator('strong').boundingBox();
        expect(text!.x - checkbox!.x - checkbox!.width).toBeGreaterThanOrEqual(8);
        expect((await control.boundingBox())!.height).toBeGreaterThanOrEqual(44);
      }
      await page.screenshot({ path: '../.runtime/admin-panel-evidence/' + width + '-shipping-' + type + '-dialog.png' });
      const audit = await new AxeBuilder({ page }).include('dialog[open]').withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
      expect(audit.violations).toEqual([]);
      await page.keyboard.press('Escape'); await expect(dialog).not.toBeVisible();
    }
  });
}

async function operations(page: Page, owner = true) {
  const response = await page.request.post(acceptanceApi + '/__fixture/operations');
  expect(response.ok()).toBeTruthy();
  const fixture = await response.json();
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  const login = await page.request.post(acceptanceApi + '/api/v1/auth/staff/login', { data: { email: owner ? fixture.superAdminEmail : fixture.basic.adminEmail, password: fixture.basic.password, authenticatorCode: code } });
  expect(login.ok()).toBeTruthy();
  return fixture;
}

test('missing product and media thumbnails have a bounded accessible fallback', async ({ page }) => {
  await operations(page);
  await page.route('**/__fixture/image/**', route => route.fulfill({ status: 404 }));
  for (const route of ['products', 'media']) {
    await page.goto('./admin/' + route);
    const fallback = page.getByRole('img', { name: /image unavailable$/ }).first();
    await expect(fallback).toBeVisible();
    expect((await fallback.boundingBox())!.width).toBe(48);
    expect((await fallback.boundingBox())!.height).toBe(48);
  }
});

test('customer suspension, blocking, reactivation and removal review preserve order records', async ({ page, browser }) => {
  const fixture = await operations(page);
  const customerContext = await browser.newContext();
  try {
    const customer = await customerContext.newPage();
    const signIn = await customer.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.basic.customerEmail, password: fixture.basic.password } });
    expect(signIn.ok()).toBeTruthy();
    const oldHeaders = { Authorization: 'Bearer ' + (await signIn.json()).token };
    await page.goto('./admin/customers');
    await page.getByLabel('Search name or email', { exact: true }).fill(fixture.basic.customerEmail);
    const customerRow = page.getByRole('row').filter({ hasText: fixture.basic.customerEmail });
    await expect(customerRow).toContainText('Active');
    await customerRow.getByRole('link').click();
    await expect(page.getByRole('table', { name: 'Recent customer orders (up to 50)', exact: true })).toContainText('REFUND-FIXTURE-');
    for (const action of ['Suspend customer', 'Reactivate customer', 'Block customer']) {
      await page.getByRole('button', { name: action, exact: true }).click();
      const dialog = page.getByRole('dialog', { name: action, exact: true });
      await dialog.getByLabel('Reason', { exact: true }).fill('Isolated browser account-access regression');
      await dialog.getByRole('button', { name: action, exact: true }).click();
      await expect(dialog).not.toBeVisible();
      const status = action === 'Suspend customer' ? 'Suspended' : action === 'Block customer' ? 'Blocked' : 'Active';
      await expect(page.locator('aside[aria-label="Customer summary"]')).toContainText(status);
    }
    expect((await customer.request.get(acceptanceApi + '/api/v1/orders', { headers: oldHeaders })).status()).toBe(401);
    expect((await customer.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.basic.customerEmail, password: fixture.basic.password } })).status()).toBe(401);
    await page.getByRole('button', { name: 'Reactivate customer', exact: true }).click();
    let dialog = page.getByRole('dialog', { name: 'Reactivate customer', exact: true });
    await dialog.getByLabel('Reason', { exact: true }).fill('Restore after isolated browser regression');
    await dialog.getByRole('button', { name: 'Reactivate customer', exact: true }).click();
    await expect(dialog).not.toBeVisible();
    expect((await customer.request.get(acceptanceApi + '/api/v1/orders', { headers: oldHeaders })).status()).toBe(401);
    expect((await customer.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.basic.customerEmail, password: fixture.basic.password } })).ok()).toBeTruthy();
    await page.getByRole('button', { name: 'Request account removal', exact: true }).click();
    dialog = page.getByRole('dialog', { name: 'Request account removal', exact: true });
    await dialog.getByLabel('Reason', { exact: true }).fill('Retention review requested for isolated fixture');
    await expect(dialog.getByRole('button', { name: 'Request account removal', exact: true })).toBeDisabled();
    await dialog.getByRole('checkbox').check();
    await dialog.getByRole('button', { name: 'Request account removal', exact: true }).click();
    await expect(dialog).not.toBeVisible();
    await expect(page.getByRole('table', { name: 'Account-removal review requests', exact: true })).toContainText('Pending');
    await expect(page.getByRole('table', { name: 'Recent customer orders (up to 50)', exact: true })).toContainText('REFUND-FIXTURE-');
    await expect(page.getByRole('table', { name: 'Recent account access history (up to 30)', exact: true })).toContainText('Retention review requested');
  } finally { await customerContext.close(); }
});

test('a real customer review publishes, hides, restores and is removed with rating and audit consistency', async ({ page, browser }) => {
  const fixture = await operations(page, false);
  const customer = await browser.newContext();
  try {
    const login = await customer.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.basic.customerEmail, password: fixture.basic.password } });
    expect(login.ok()).toBeTruthy();
    const headers = { Authorization: 'Bearer ' + (await login.json()).token };
    const product = fixture.basic.products[1];
    const comment = 'Browser-published feedback ' + Date.now();
    const created = await customer.request.post(acceptanceApi + '/api/v1/products/' + product + '/reviews', { headers, data: { rating: 4, comment } });
    expect(created.status()).toBe(201); expect((await created.json()).moderationStatus).toBe('Published');
    const summaries = async () => (await customer.request.get(acceptanceApi + '/api/v1/products/' + product + '/reviews')).json();
    const before = await summaries(); expect(before.reviews.some((review: { comment: string }) => review.comment === comment)).toBe(true);
    await page.goto('./admin/reviews');
    await page.getByLabel('Search reviews', { exact: true }).fill(comment);
    const row = page.getByRole('row').filter({ hasText: comment });
    for (const action of ['Hide review', 'Restore review', 'Remove review']) {
      await row.getByRole('button', { name: action, exact: true }).click();
      const dialog = page.getByRole('dialog', { name: action, exact: true });
      await dialog.getByLabel('Moderation reason', { exact: true }).fill(action + ' for isolated browser verification');
      await dialog.getByRole('button', { name: action, exact: true }).click();
      await expect(dialog).not.toBeVisible();
      await expect(row).toContainText(action === 'Restore review' ? 'Published' : action === 'Hide review' ? 'Hidden' : 'Removed');
      const summary = await summaries();
      expect(summary.totalCount).toBe(before.totalCount - (action === 'Restore review' ? 0 : 1));
      expect(summary.reviews.some((review: { comment: string }) => review.comment === comment)).toBe(action === 'Restore review');
      expect(summary.ratingDistribution4).toBe(before.ratingDistribution4 - (action === 'Restore review' ? 0 : 1));
    }
    await row.getByRole('button', { name: 'View history', exact: true }).click();
    const history = page.getByRole('dialog', { name: /Review .* history/ });
    await expect(history.getByRole('region', { name: 'Review details', exact: true })).toContainText(comment);
    await expect(history).toContainText('4 out of 5');
    await expect(history).toContainText('ReviewPublishedByOwner'); await expect(history).toContainText('Remove review for isolated browser verification');
    for (const width of [1440, 390]) {
      await page.setViewportSize({ width, height: 900 });
      expect(await history.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
      await page.screenshot({ path: '../.runtime/admin-panel-evidence/' + width + '-review-detail.png' });
      const audit = await new AxeBuilder({ page }).include('[role="dialog"],dialog[open]').withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa', 'wcag22aa']).analyze();
      expect(audit.violations).toEqual([]);
    }
  } finally { await customer.close(); }
});

test('owner setting changes persist and reach the real storefront footer', async ({ page, browser }) => {
  await operations(page);
  await page.goto('./admin/settings');
  await page.getByLabel('Search settings', { exact: true }).fill('Contact email');
  await page.getByRole('button', { name: /^(Edit|Configure) Contact email$/, exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Edit Contact email', exact: true });
  const email = 'store-' + Date.now() + '@example.test';
  await dialog.getByLabel('Contact email', { exact: true }).fill(email);
  await dialog.getByRole('checkbox', { name: 'Show on storefront', exact: true }).check();
  await dialog.getByRole('button', { name: 'Save setting', exact: true }).click();
  await expect(dialog).not.toBeVisible();
  await page.reload(); await expect(page.getByRole('table', { name: 'Supported store settings', exact: true })).toContainText(email);
  const storefront = await browser.newContext();
  try { const shop = await storefront.newPage(); await shop.goto('http://127.0.0.1:5178/Amaara-Creations/contact'); await expect(shop.locator('footer')).toContainText(email); } finally { await storefront.close(); }
});

test('shipping dialogs protect drafts, align controls and validate dependent ranges on mobile', async ({ page }) => {
  await operations(page);
  await page.setViewportSize({ width: 390, height: 844 });
  await page.goto('./admin/shipping');
  await expect(page.locator('form')).toHaveCount(0);
  await page.getByRole('button', { name: 'New zone', exact: true }).click();
  let dialog = page.getByRole('dialog', { name: 'New delivery zone', exact: true });
  await dialog.getByLabel('Name', { exact: true }).fill('Draft zone');
  const phone = dialog.getByRole('checkbox', { name: 'Require phone number', exact: true });
  await expect(phone).toBeChecked(); await dialog.getByText('Require phone number', { exact: true }).click(); await expect(phone).not.toBeChecked();
  for (const checkbox of await dialog.locator('.admin-check').all()) {
    const bounds = await checkbox.boundingBox(); expect(bounds!.height).toBeGreaterThanOrEqual(44);
  }
  expect(await dialog.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
  await page.keyboard.press('Escape');
  const discard = page.getByRole('dialog', { name: 'Discard changes?', exact: true });
  await expect(discard).toBeVisible(); await discard.getByRole('button', { name: 'Continue Editing', exact: true }).click();
  await expect(dialog.getByLabel('Name', { exact: true })).toHaveValue('Draft zone');
  await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
  await discard.getByRole('button', { name: 'Discard Changes', exact: true }).click();
  await expect(dialog).not.toBeVisible();
  await page.getByRole('button', { name: 'New method', exact: true }).click();
  dialog = page.getByRole('dialog', { name: 'New delivery method', exact: true });
  await dialog.getByRole('combobox', { name: 'Delivery zone', exact: true }).selectOption({ index: 1 });
  await dialog.getByLabel('Name', { exact: true }).fill('Range validation');
  await dialog.getByLabel('Code', { exact: true }).fill('RANGE_VALIDATION');
  await dialog.getByLabel('Base price', { exact: true }).fill('2');
  await dialog.getByLabel('Minimum order value', { exact: true }).fill('20');
  await dialog.getByLabel('Maximum order value', { exact: true }).fill('10');
  await expect(dialog.getByLabel('Pickup location', { exact: true })).toHaveCount(0);
  await dialog.getByRole('checkbox', { name: 'Customer pickup', exact: true }).check();
  await dialog.getByLabel('Pickup location', { exact: true }).fill('Test pickup location');
  await dialog.getByRole('button', { name: 'Save delivery method', exact: true }).click();
  await expect(dialog.getByRole('alert')).toContainText('Minimum order value cannot exceed maximum order value');
  const audit = await new AxeBuilder({ page }).include('dialog[open]').analyze();
  expect(audit.violations.filter(v => v.impact === 'critical' || v.impact === 'serious')).toEqual([]);
});
