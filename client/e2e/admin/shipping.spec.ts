import { test, expect, acceptanceApi } from '../fixtures';

test('an admin-created delivery method appears at checkout and test rates stay hidden', async ({ page, browser, fixture }) => {
  const runId = Date.now();
  const country = `Shipping fixture ${runId}`;
  const code = (await (await page.request.get(`${acceptanceApi}/__fixture/authenticator-code`)).json()).code;
  const staffLogin = await page.request.post(`${acceptanceApi}/api/v1/auth/staff/login`, {
    data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code },
  });
  expect(staffLogin.ok()).toBeTruthy();
  const staffHeaders = { Authorization: `Bearer ${(await staffLogin.json()).token}` };

  const customerContext = await browser.newContext({ baseURL: 'http://127.0.0.1:5178/Amaara-Creations/' });
  try {
    const customer = await customerContext.newPage();
    const customerLogin = await customer.request.post(`${acceptanceApi}/api/v1/auth/login`, {
      data: { email: fixture.customerEmail, password: fixture.password },
    });
    expect(customerLogin.ok()).toBeTruthy();
    const customerHeaders = { Authorization: `Bearer ${(await customerLogin.json()).token}` };
    expect((await customer.request.post(`${acceptanceApi}/api/v1/addresses`, {
      headers: customerHeaders,
      data: { label: 'Shipping regression', recipientName: 'Test customer', addressLine1: 'Test street', city: 'Test city', country, phone: '+94770000000', isDefault: true },
    })).ok()).toBeTruthy();
    expect((await customer.request.post(`${acceptanceApi}/api/v1/cart`, {
      headers: customerHeaders,
      data: { productId: fixture.products[0], quantity: 1 },
    })).ok()).toBeTruthy();
    await customer.goto('./checkout');
    const shipping = customer.getByRole('combobox', { name: 'Shipping method', exact: true });
    await expect(customer.getByRole('alert')).toContainText('No delivery option');
    await expect(shipping).toBeDisabled();
    await expect(customer.getByRole('button', { name: 'Pay securely', exact: true })).toBeDisabled();

    await page.goto('./admin/shipping');
    await page.getByRole('button', { name: 'New zone', exact: true }).click();
    const zoneForm = page.getByRole('dialog', { name: 'New delivery zone', exact: true });
    await zoneForm.getByLabel('Name', { exact: true }).fill(`Shipping regression ${runId}`);
    await zoneForm.getByLabel('Country', { exact: true }).fill(country);
    const zoneResponse = page.waitForResponse(response => response.url().endsWith('/admin/shipping/zones') && response.request().method() === 'POST');
    await zoneForm.getByRole('button', { name: 'Save zone', exact: true }).click();
    const zone = await (await zoneResponse).json();
    await expect(zoneForm).not.toBeVisible();
    await page.getByRole('button', { name: 'New method', exact: true }).click();
    const methodForm = page.getByRole('dialog', { name: /delivery method$/, exact: false });
    await methodForm.getByRole('combobox', { name: 'Delivery zone', exact: true }).selectOption(String(zone.id));
    const testRate = methodForm.getByRole('checkbox', { name: 'Test rate (excluded from regular checkout)', exact: true });
    await expect(testRate).not.toBeChecked();
    await methodForm.getByLabel('Name', { exact: true }).fill('Browser fixture delivery');
    const methodCode = `SHIPPING_TEST_${runId}`;
    await methodForm.getByLabel('Code', { exact: true }).fill(methodCode);
    await methodForm.getByLabel('Base price', { exact: true }).fill('3.50');
    const createdResponse = page.waitForResponse(response => response.url().endsWith('/admin/shipping/methods') && response.request().method() === 'POST');
    await methodForm.getByRole('button', { name: 'Save delivery method', exact: true }).click();
    const created = await (await createdResponse).json();
    await expect(page.getByRole('status').filter({ hasText: 'Delivery method saved.' })).toContainText('available at checkout');
    await expect(methodForm).not.toBeVisible();

    await customer.getByRole('button', { name: 'Reload delivery options', exact: true }).click();
    await expect(shipping).toBeEnabled();
    await expect(shipping.locator('option').filter({ hasText: 'Browser fixture delivery' })).toContainText('3.50');
    await shipping.selectOption(String(created.id));
    await expect(customer.getByRole('button', { name: 'Pay securely', exact: true })).toBeEnabled();

    await page.getByRole('row').filter({ hasText: methodCode }).getByRole('button', { name: 'Edit method', exact: true }).click();
    await testRate.check();
    const updatedResponse = page.waitForResponse(response => response.url().endsWith(`/admin/shipping/methods/${created.id}`) && response.request().method() === 'PUT');
    await methodForm.getByRole('button', { name: 'Save delivery method', exact: true }).click();
    expect((await updatedResponse).ok()).toBeTruthy();
    await expect(page.getByRole('status').filter({ hasText: 'Delivery method saved as a test rate.' })).toContainText('excluded from regular checkout');
    await customer.reload();
    await expect(customer.getByRole('alert')).toContainText('No delivery option');
    await expect(shipping).toBeDisabled();

    const configuration = await (await page.request.get(`${acceptanceApi}/api/v1/admin/shipping`, { headers: staffHeaders })).json();
    expect(configuration.methods.filter((method: { code: string }) => method.code === methodCode)).toHaveLength(1);
  } finally {
    await customerContext.close();
  }
});
