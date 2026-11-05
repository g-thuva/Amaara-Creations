import { test, expect, storefrontUrl, managementUrl, acceptanceApi, type Page } from '../fixtures';
import { fillAuthenticator } from '../staffAuth';

async function signIn(page: Page, fixture: { adminEmail: string; password: string }) {
  await page.goto(storefrontUrl + 'login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.adminEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await fillAuthenticator(page);
  await page.getByRole('button', { name: 'Verify and sign in', exact: true }).click();
}

test('paired development startup opens the staff dashboard after shared sign-in and MFA', async ({ page, fixture }) => {
  await signIn(page, fixture);
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  expect(new URL(page.url()).origin).toBe(new URL(managementUrl).origin);
  await page.reload();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
});

for (const customerSession of [false, true]) {
  test(`an unavailable local staff page preserves completed MFA and retries without credentials (customer session: ${customerSession})`, async ({ page, fixture }) => {
    if (customerSession) {
      const customer = await page.request.post(acceptanceApi + '/api/v1/auth/login', { data: { email: fixture.customerEmail, password: fixture.password } });
      expect(customer.ok()).toBe(true);
    }
    let passwords = 0; let codes = 0;
    page.on('request', request => {
      if (request.method() !== 'POST') return;
      if (request.url().endsWith('/auth/sign-in')) passwords++;
      if (request.url().endsWith('/auth/sign-in/verify')) codes++;
    });
    const destination = managementUrl + 'login*';
    await page.route(destination, route => route.request().isNavigationRequest() ? route.continue() : route.abort('connectionrefused'));
    await signIn(page, fixture);
    await expect(page.getByRole('heading', { name: 'Your sign-in is complete', exact: true })).toBeVisible();
    await expect(page.getByRole('alert')).toContainText('staff workspace is currently unavailable');
    await expect(page).toHaveURL(storefrontUrl + 'login');
    await expect(page.getByLabel('Password', { exact: true })).toHaveCount(0);
    await expect(page.getByLabel('Authentication code', { exact: true })).toHaveCount(0);
    await page.unroute(destination);
    await page.getByRole('button', { name: 'Open staff workspace', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
    expect(passwords).toBe(1); expect(codes).toBe(1);
  });
}

test('customer sign-in does not probe or open the staff workspace', async ({ page, fixture }) => {
  const staffRequests: string[] = [];
  page.on('request', request => { if (new URL(request.url()).origin === new URL(managementUrl).origin) staffRequests.push(request.url()); });
  await page.goto(storefrontUrl + 'login');
  await page.getByLabel('Email Address', { exact: true }).fill(fixture.customerEmail);
  await page.getByLabel('Password', { exact: true }).fill(fixture.password);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  expect(staffRequests).toEqual([]);
});
