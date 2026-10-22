import { test, expect, acceptanceApi } from '../fixtures';

test('a real selected product option keeps its identity and current price in the cart', async ({ page, request, fixture }) => {
  const code = (await (await request.get(`${acceptanceApi}/__fixture/authenticator-code`)).json()).code;
  const admin = await request.post(`${acceptanceApi}/api/v1/auth/staff/login`, { data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code } });
  expect(admin.ok()).toBeTruthy();
  const headers = { Authorization: `Bearer ${(await admin.json()).token}` };
  const productId = fixture.products[0];
  const name = `TEST option ${Date.now()}`;
  const created = await request.post(`${acceptanceApi}/api/v1/admin/products/${productId}/variants`, { headers, data: { name, sku: `TEST-${Date.now()}`, priceOverride: 173, stockQuantity: 4, isActive: true } });
  expect(created.ok()).toBeTruthy();
  const variant = await created.json();
  try {
    expect((await page.request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } })).ok()).toBeTruthy();
    await page.goto(`./products/${productId}`);
    await page.getByRole('combobox', { name: 'Product option', exact: true }).selectOption(String(variant.id));
    await page.getByRole('spinbutton').fill('2');
    await page.getByRole('button', { name: 'Add to cart', exact: true }).first().click();
    await expect(page.getByText('Added to your cart.', { exact: true })).toBeVisible();
    await page.goto('./cart');
    await expect(page.getByRole('heading', { name: new RegExp(name) })).toBeVisible();
    await expect(page.getByRole('spinbutton')).toHaveValue('2');
    const auth = await page.request.post(`${acceptanceApi}/api/v1/auth/refresh`);
    expect(auth.ok()).toBeTruthy();
    const cart = await request.get(`${acceptanceApi}/api/v1/cart`, { headers: { Authorization: `Bearer ${(await auth.json()).token}` } });
    expect(cart.ok()).toBeTruthy();
    expect((await cart.json()).items[0]).toMatchObject({ productId, productVariantId: variant.id, variantName: name, productPrice: 173, quantity: 2, subtotal: 346 });
    await page.reload();
    await expect(page.getByRole('heading', { name: new RegExp(name) })).toBeVisible();
  } finally {
    expect((await request.post(`${acceptanceApi}/api/v1/admin/products/${productId}/variants/${variant.id}/archive`, { headers })).ok()).toBeTruthy();
  }
});
