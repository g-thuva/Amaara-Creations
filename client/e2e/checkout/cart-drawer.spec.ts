import { test, expect, acceptanceApi } from '../fixtures';

test('cart preview supports Escape focus return, full-cart navigation and safe remove undo', async ({ page, fixture }) => {
  const login = await page.request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } }); expect(login.ok()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await login.json()).token}` };
  expect((await page.request.post(`${acceptanceApi}/api/v1/cart`, { headers, data: { productId: fixture.products[0], quantity: 2 } })).ok()).toBeTruthy();
  await page.setViewportSize({ width: 320, height: 900 });
  await page.goto('./');
  const trigger = page.getByRole('link', { name: 'Cart, 2 items', exact: true });
  await trigger.click();
  const drawer = page.getByRole('dialog', { name: 'Cart preview', exact: true });
  await expect(drawer).toBeVisible();
  await expect(drawer.locator('li')).toHaveCount(1);
  expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth)).toBe(true);
  await page.keyboard.press('Escape'); await expect(drawer).not.toBeVisible(); await expect(trigger).toBeFocused();
  await trigger.click(); await drawer.getByRole('link', { name: 'View and edit cart', exact: true }).click();
  await expect(page).toHaveURL(/\/cart$/);
  await page.getByRole('button', { name: 'Remove', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Confirm', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Your cart is ready for a favourite', exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Restore removed item', exact: true }).click();
  await expect(page.getByRole('spinbutton')).toHaveValue('2');
  await page.reload(); await expect(page.getByRole('spinbutton')).toHaveValue('2');
});

test('custom artwork thumbnails use authorized blob reads in both cart views', async ({ page, request, fixture }) => {
  expect((await page.request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } })).ok()).toBeTruthy();
  await page.goto('./custom');
  await expect(page.getByText('Custom item subtotal', { exact: true })).toBeVisible();
  await page.getByLabel('Artwork (JPEG, PNG or WebP)', { exact: true }).setInputFiles({ name: 'private-cart.png', mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64') });
  await expect(page.getByText('private-cart.png', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Save and add to cart', exact: true }).click();
  await expect(page).toHaveURL(/\/cart$/);
  await expect(page.locator('.s-cart-image img')).toHaveAttribute('src', /^blob:/);
  await page.getByRole('link', { name: /^Cart, / }).click();
  await expect(page.getByRole('dialog', { name: 'Cart preview', exact: true }).locator('img')).toHaveAttribute('src', /^blob:/);
  const refresh = await page.request.post(`${acceptanceApi}/api/v1/auth/refresh`);
  const cart = await page.request.get(`${acceptanceApi}/api/v1/cart`, { headers: { Authorization: `Bearer ${(await refresh.json()).token}` } });
  const url = (await cart.json()).items[0].artworkDownloadUrl;
  expect(url).toMatch(/^\/api\/v1\/custom-designs\/\d+\/assets\/\d+$/);
  expect((await request.get(`${acceptanceApi}${url}`)).status()).toBe(401);
});
