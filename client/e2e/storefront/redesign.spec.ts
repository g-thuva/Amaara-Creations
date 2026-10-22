import { test, expect, acceptanceApi } from '../fixtures';

test('every password field can be revealed independently without changing autocomplete', async ({ page }) => {
  for (const route of ['login', 'register', 'reset-password']) {
    await page.goto(`./${route}`);
    const controls = page.locator('.s-password-field');
    await expect(controls.first()).toBeVisible();
    for (const field of await controls.all()) {
      const input = field.locator('input');
      await input.fill('a unique printing passphrase');
      const autocomplete = await input.getAttribute('autocomplete');
      await expect(input).toHaveAttribute('type', 'password');
      await field.getByRole('button').click();
      await expect(input).toHaveAttribute('type', 'text');
      await expect(field.getByRole('button')).toHaveAttribute('aria-pressed', 'true');
      await field.getByRole('button').click();
      await expect(input).toHaveAttribute('type', 'password');
      expect(autocomplete).toBe(route === 'login' ? 'current-password' : 'new-password');
    }
  }
});

test('empty inventory hides filters while no matches keeps recovery actions', async ({ page }) => {
  await page.route('**/api/v1/products?*', route => route.fulfill({ json: { products: [], totalCount: 0, publishedCount: 0, totalPages: 0, currency: 'LKR' } }));
  await page.goto('./products');
  await expect(page.getByRole('heading', { name: 'Begin with your own design' })).toBeVisible();
  await expect(page.getByRole('complementary', { name: 'Product filters' })).toHaveCount(0);
  await expect(page.getByRole('button', { name: /^Filters/ })).toHaveCount(0);
  await page.unroute('**/api/v1/products?*');
  await page.goto('./products?search=no-match-ever-974132');
  await expect(page.getByRole('heading', { name: 'No products match those filters' })).toBeVisible();
  await expect(page.getByRole('region', { name: 'Products', exact: true }).getByRole('button', { name: 'Clear filters', exact: true })).toBeVisible();
});

test('preview matches shape and proportion, uses singular quantity and restores private art', async ({ page, fixture }) => {
  await page.goto('./custom');
  await expect(page.getByText('Custom item subtotal')).toBeVisible();
  await page.getByRole('combobox', { name: 'Shape', exact: true }).selectOption('rectangle');
  await page.getByLabel(/^Width/).fill('8');
  await page.getByLabel(/^Height/).fill('4');
  await page.getByLabel('Quantity', { exact: true }).fill('1');
  const preview = page.locator('.s-sticker-preview');
  await expect(preview).toHaveAttribute('data-shape', 'rectangle');
  const box = await preview.boundingBox();
  expect(box!.width / box!.height).toBeCloseTo(2, 1);
  await expect(page.getByLabel('Sticker preview')).toContainText('1 sticker');
  await expect(page.getByLabel('Sticker preview')).not.toContainText('1 stickers');
  await page.getByRole('combobox', { name: 'Shape', exact: true }).selectOption('round');
  await expect(page.getByLabel(/^Height/)).toHaveValue('8');
  await expect(preview).toHaveAttribute('data-shape', 'circle');
  const circle = await preview.boundingBox();
  expect(circle!.width / circle!.height).toBeCloseTo(1, 1);
  const response = await page.request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } });
  expect(response.ok()).toBeTruthy();
  await page.reload();
  await page.getByRole('button', { name: 'Save design', exact: true }).click();
  await expect(page).toHaveURL(/\/custom\/\d+$/);
  await page.getByLabel('Artwork (JPEG, PNG or WebP)').setInputFiles({ name: 'private.png', mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64') });
  await expect(page.getByText('private.png', { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByAltText('Selected artwork preview')).toBeVisible();
  await expect(page.getByAltText('Selected artwork preview')).toHaveAttribute('src', /^blob:/);
});

test('customer and staff shoppers have no staff links or routes in the storefront', async ({ page, fixture }) => {
  const code = (await (await page.request.get(`${acceptanceApi}/__fixture/authenticator-code`)).json()).code;
  const signedIn = await page.request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code } });
  expect(signedIn.ok()).toBeTruthy();
  await page.goto('./profile');
  await expect(page.getByRole('heading', { name: 'Your profile', exact: true })).toBeVisible();
  expect(await page.locator('a[href*="/admin"]').count()).toBe(0);
  await page.goto('./admin/orders');
  await expect(page.getByRole('heading', { name: 'Page not found', exact: true })).toBeVisible();
});
