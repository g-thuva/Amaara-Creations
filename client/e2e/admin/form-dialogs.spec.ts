import AxeBuilder from '@axe-core/playwright';
import { test, expect, acceptanceApi, type Page } from '../fixtures';

const png = Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64');
const name = () => `Dialog QA ${Date.now()}-${Math.random().toString(36).slice(2, 6)}`;

test.beforeEach(async ({ page, fixture }) => {
  const code = (await (await page.request.get(acceptanceApi + '/__fixture/authenticator-code')).json()).code;
  const login = await page.request.post(acceptanceApi + '/api/v1/auth/staff/login', {
    data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code },
  });
  expect(login.ok()).toBeTruthy();
});

async function authorization(page: Page) {
  const response = await page.request.post(acceptanceApi + '/api/v1/auth/staff/refresh');
  expect(response.ok()).toBeTruthy();
  return { Authorization: 'Bearer ' + (await response.json()).token };
}

async function productDialog(page: Page) {
  await page.goto('./admin/products');
  await expect(page.getByRole('table', { name: 'Product catalog', exact: true })).toBeVisible();
  await expect(page.getByLabel('Product name', { exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: 'New Product', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'New Product', exact: true });
  await expect(dialog).toBeVisible();
  await expect(dialog.getByLabel('Product name', { exact: true })).toBeFocused();
  await expect(dialog.getByRole('button', { name: 'Create Product', exact: true })).toBeEnabled();
  return dialog;
}

async function fillProduct(page: Page, value = name()) {
  await page.getByLabel('Product name', { exact: true }).fill(value);
  await page.getByLabel('Full description', { exact: true }).fill('Disposable product for admin form verification.');
  await page.getByLabel('Base price', { exact: true }).fill('29.75');
  await page.getByLabel('Stock', { exact: true }).fill('8');
  return value;
}

async function dismiss(page: Page, method: string) {
  const dialog = page.locator('dialog.admin-form-dialog[open]');
  if (method === 'backdrop') await page.mouse.click(2, 2);
  if (method === 'escape') await page.keyboard.press('Escape');
  if (method === 'cancel') await dialog.getByRole('button', { name: 'Cancel', exact: true }).click();
  if (method === 'close') await dialog.getByRole('button', { name: /^Close / }).click();
}

for (const method of ['backdrop', 'escape', 'cancel', 'close']) {
  test(`product ${method} dismisses a clean form and protects a changed form`, async ({ page }) => {
    let dialog = await productDialog(page);
    await dialog.getByLabel('Product name', { exact: true }).click();
    await expect(dialog).toBeVisible();
    await dismiss(page, method);
    await expect(dialog).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'New Product', exact: true })).toBeFocused();
    await expect.poll(() => page.evaluate(() => document.body.style.overflow)).not.toBe('hidden');

    await page.getByRole('button', { name: 'New Product', exact: true }).click();
    dialog = page.getByRole('dialog', { name: 'New Product', exact: true });
    await dialog.getByLabel('Product name', { exact: true }).fill('Keep my product draft');
    await dismiss(page, method);
    const confirmation = page.getByRole('dialog', { name: 'Discard changes?', exact: true });
    await expect(confirmation).toBeVisible();
    await expect(confirmation.getByRole('button', { name: 'Continue Editing', exact: true })).toBeFocused();
    if (method === 'backdrop') {
      const accessibility = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze();
      expect(accessibility.violations.map(violation => violation.id)).toEqual([]);
    }
    await confirmation.getByRole('button', { name: 'Continue Editing', exact: true }).click();
    await expect(dialog.getByLabel('Product name', { exact: true })).toHaveValue('Keep my product draft');
    await expect.poll(() => page.evaluate(() => document.body.style.overflow)).toBe('hidden');
    await dismiss(page, method);
    await confirmation.getByRole('button', { name: 'Discard Changes', exact: true }).click();
    await expect(page.locator('dialog[open]')).toHaveCount(0);
    await expect.poll(() => page.evaluate(() => document.body.style.overflow)).not.toBe('hidden');
    await page.getByRole('button', { name: 'New Product', exact: true }).click();
    await expect(page.getByLabel('Product name', { exact: true })).toHaveValue('');
  });
}

test('required and server validation preserve the draft and never create an invalid product', async ({ page }) => {
  const dialog = await productDialog(page);
  let posts = 0;
  await page.route('**/api/v1/admin/products', async route => {
    if (route.request().method() !== 'POST') return route.continue();
    posts++;
    return route.fulfill({ status: 400, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Validation failed', status: 400, code: 'VALIDATION_ERROR' }) });
  });
  await dialog.getByRole('button', { name: 'Create Product', exact: true }).click();
  expect(await dialog.getByLabel('Product name', { exact: true }).evaluate(input => (input as HTMLInputElement).validity.valueMissing)).toBe(true);
  expect(posts).toBe(0);
  const value = await fillProduct(page);
  await dialog.getByRole('button', { name: 'Create Product', exact: true }).click();
  await expect(dialog.getByRole('alert')).toBeVisible();
  await expect(dialog.getByLabel('Product name', { exact: true })).toHaveValue(value);
  await expect(dialog.getByLabel('Base price', { exact: true })).toHaveValue('29.75');
  expect(posts).toBe(1);
});

test('saving creates exactly one product, refreshes the count and keeps filters', async ({ page }) => {
  await page.goto('./admin/products');
  await page.getByLabel('Search products', { exact: true }).fill('A filter that hides the new record');
  await page.getByRole('button', { name: 'New Product', exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'New Product', exact: true });
  const value = await fillProduct(page);
  let release!: () => void;
  const gate = new Promise<void>(resolve => { release = resolve; });
  let posts = 0;
  await page.route('**/api/v1/admin/products', async route => {
    if (route.request().method() === 'POST') { posts++; await gate; }
    await route.continue();
  });
  await dialog.getByRole('button', { name: 'Create Product', exact: true }).click();
  await expect(dialog.getByRole('button', { name: 'Saving…', exact: true })).toBeDisabled();
  await expect(dialog.getByRole('button', { name: 'Cancel', exact: true })).toBeDisabled();
  await dialog.locator('form').evaluate(form => { (form as HTMLFormElement).requestSubmit(); (form as HTMLFormElement).requestSubmit(); });
  release();
  await expect(dialog).toHaveCount(0);
  expect(posts).toBe(1);
  await expect(page.getByLabel('Search products', { exact: true })).toHaveValue('A filter that hides the new record');
  await expect(page.getByRole('status').filter({ hasText: value })).toBeVisible();
  await page.getByRole('button', { name: 'Find saved product', exact: true }).click();
  await expect(page.getByRole('row').filter({ hasText: value })).toHaveCount(1);
  await expect(page.locator('.header-content p')).toHaveText('1 products');
  await page.getByRole('button', { name: 'New Product', exact: true }).click();
  await expect(page.getByLabel('Product name', { exact: true })).toHaveValue('');
});

test('media previews, drop, ordering and removal survive rerenders and persist through the real API', async ({ page }) => {
  const dialog = await productDialog(page);
  const value = await fillProduct(page);
  await page.getByLabel('Product images and videos', { exact: true }).setInputFiles({ name: 'first.png', mimeType: 'image/png', buffer: png });
  await page.getByLabel('Caption / alt text 1', { exact: true }).fill('First image');
  await page.locator('.pf-dropzone').evaluate((drop, image) => {
    const transfer = new DataTransfer();
    transfer.items.add(new File([Uint8Array.from(atob(image), char => char.charCodeAt(0))], 'second.png', { type: 'image/png' }));
    drop.dispatchEvent(new DragEvent('drop', { bubbles: true, cancelable: true, dataTransfer: transfer }));
  }, png.toString('base64'));
  await expect(dialog.locator('.pf-preview-card')).toHaveCount(2);
  await page.getByLabel('Caption / alt text 2', { exact: true }).fill('Second image');
  await page.getByRole('button', { name: 'Move second.png earlier', exact: true }).click();
  await expect(dialog.locator('.pf-preview-card').first()).toContainText('second.png');
  await page.getByLabel('Product name', { exact: true }).fill(value + ' with photos');
  await expect(dialog.locator('.pf-preview-card img').first()).toBeVisible();
  await page.getByLabel('Product images and videos', { exact: true }).setInputFiles({ name: 'clip.webm', mimeType: 'video/webm', buffer: Buffer.from('video-preview-only') });
  await expect(dialog.locator('video')).toHaveCount(1);
  await page.getByRole('button', { name: 'Remove clip.webm', exact: true }).click();
  await dialog.getByRole('button', { name: 'Create Product', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await page.getByRole('button', { name: 'Find saved product', exact: true }).click();
  await page.getByRole('button', { name: `Edit ${value} with photos`, exact: true }).click();
  const edit = page.getByRole('dialog', { name: 'Edit Product', exact: true });
  await expect(edit.locator('.pf-existing-media img')).toHaveCount(2);
  await expect(edit.locator('.pf-existing-media img').first()).toHaveAttribute('alt', 'Second image');
  await expect(edit.getByRole('combobox', { name: 'Currency', exact: true })).toBeDisabled();
  await expect(edit.getByLabel('Stock', { exact: true })).toBeDisabled();
  await edit.getByLabel('Base price', { exact: true }).fill('42.50');
  await edit.getByRole('button', { name: 'Save Changes', exact: true }).click();
  await expect(edit).toHaveCount(0);
  await expect(page.getByRole('row').filter({ hasText: value })).toContainText('42.50');
});

test('retrying a failed media upload preserves the selection without duplicating the saved product', async ({ page }) => {
  const dialog = await productDialog(page);
  const value = await fillProduct(page);
  await page.getByLabel('Product images and videos', { exact: true }).setInputFiles({ name: 'retry.png', mimeType: 'image/png', buffer: png });
  let creates = 0;
  let uploads = 0;
  page.on('request', request => { if (request.method() === 'POST' && request.url().endsWith('/admin/products')) creates++; });
  await page.route('**/api/v1/admin/products/*/media', async route => {
    if (route.request().method() === 'POST' && ++uploads === 1) return route.fulfill({ status: 503, contentType: 'application/json', body: '{"title":"Upload temporarily unavailable"}' });
    return route.continue();
  });
  await dialog.getByRole('button', { name: 'Create Product', exact: true }).click();
  await expect(dialog.getByRole('alert')).toBeVisible();
  await expect(dialog.locator('.pf-preview-card img')).toBeVisible();
  await dialog.getByRole('button', { name: 'Retry media upload', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  expect(creates).toBe(1);
  expect(uploads).toBe(2);
  await page.getByRole('button', { name: 'Find saved product', exact: true }).click();
  await expect(page.getByRole('row').filter({ hasText: value })).toHaveCount(1);
});

test('a long catalogue stays stationary and opening or typing never reloads its products', async ({ page }) => {
  const headers = await authorization(page);
  for (let index = 0; index < 22; index++) {
    const response = await page.request.post(acceptanceApi + '/api/v1/admin/products', { headers, data: { name: name(), description: 'Long catalogue test', basePrice: 7, currency: 'CHF', stock: 1, isActive: true, isFeatured: false, collectionIds: [] } });
    expect(response.ok()).toBeTruthy();
  }
  await page.goto('./admin/products');
  await expect(page.getByRole('table', { name: 'Product catalog', exact: true }).getByRole('row')).toHaveCount(21);
  const catalogue = page.locator('.table-responsive').first();
  await catalogue.evaluate(element => { element.scrollTop = element.scrollHeight; });
  const catalogueScroll = await catalogue.evaluate(element => element.scrollTop);
  expect(catalogueScroll).toBeGreaterThan(300);
  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  const scroll = await page.evaluate(() => scrollY);
  let reads = 0;
  page.on('request', request => { if (request.method() === 'GET' && new URL(request.url()).pathname.endsWith('/admin/products')) reads++; });
  const trigger = page.getByRole('button', { name: 'New Product', exact: true });
  await trigger.click();
  const dialog = page.getByRole('dialog', { name: 'New Product', exact: true });
  await expect(dialog).toBeVisible();
  expect(await page.evaluate(() => scrollY)).toBe(scroll);
  await page.getByLabel('Product name', { exact: true }).fill('Unrelated to catalogue state');
  await dialog.locator('.admin-form-dialog-body').evaluate(body => { body.scrollTop = body.scrollHeight; });
  await expect(dialog.getByRole('button', { name: 'Create Product', exact: true })).toBeInViewport();
  expect(await page.evaluate(() => scrollY)).toBe(scroll);
  expect(reads).toBe(0);
  expect(await catalogue.evaluate(element => element.scrollTop)).toBe(catalogueScroll);
  await dismiss(page, 'escape');
  await page.getByRole('button', { name: 'Discard Changes', exact: true }).click();
  expect(await page.evaluate(() => scrollY)).toBe(scroll);
  await expect(trigger).toBeFocused();
  expect(await catalogue.evaluate(element => element.scrollTop)).toBe(catalogueScroll);
});

test('category creation and editing preserve hierarchy, validate fields and refresh the list', async ({ page, fixture }) => {
  await page.goto('./admin/categories');
  await expect(page.getByRole('table', { name: 'Categories', exact: true })).toBeVisible();
  await expect(page.getByLabel('Name', { exact: true })).toHaveCount(0);
  await page.getByRole('button', { name: 'New Category', exact: true }).click();
  let dialog = page.getByRole('dialog', { name: 'New Category', exact: true });
  await dialog.getByRole('button', { name: 'Create Category', exact: true }).click();
  await expect(dialog).toBeVisible();
  const value = name();
  await dialog.getByLabel('Name', { exact: true }).fill(value);
  await dialog.getByRole('combobox', { name: 'Parent', exact: true }).selectOption(String(fixture.category));
  await dialog.getByRole('textbox', { name: 'Description', exact: true }).fill('Category description');
  await dismiss(page, 'backdrop');
  await page.getByRole('button', { name: 'Continue Editing', exact: true }).click();
  await dialog.getByRole('button', { name: 'Create Category', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  const row = page.getByRole('row').filter({ hasText: value });
  await expect(row).toBeVisible();
  await row.getByRole('button', { name: `Edit ${value}`, exact: true }).click();
  dialog = page.getByRole('dialog', { name: 'Edit Category', exact: true });
  await expect(dialog.getByRole('combobox', { name: 'Parent', exact: true })).toHaveValue(String(fixture.category));
  await dialog.getByRole('textbox', { name: 'Description', exact: true }).fill('Updated category description');
  await dialog.getByRole('button', { name: 'Save Changes', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await row.getByRole('button', { name: `Edit ${value}`, exact: true }).click();
  await expect(page.getByRole('textbox', { name: 'Description', exact: true })).toHaveValue('Updated category description');
});

test('collections preserve product associations when editing metadata', async ({ page, fixture }) => {
  await page.goto('./admin/collections');
  await page.getByRole('button', { name: 'New Collection', exact: true }).click();
  const value = name();
  await page.getByLabel('Name', { exact: true }).fill(value);
  await page.getByRole('listbox', { name: 'Products', exact: true }).selectOption(String(fixture.products[0]));
  await page.getByRole('button', { name: 'Create Collection', exact: true }).click();
  const row = page.getByRole('row').filter({ hasText: value });
  await expect(row).toBeVisible();
  await row.getByRole('button', { name: `Edit ${value}`, exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Edit Collection', exact: true });
  await expect(dialog.getByRole('button', { name: 'Save Changes', exact: true })).toBeEnabled();
  await expect(dialog.getByRole('listbox', { name: 'Products', exact: true })).toHaveValues([String(fixture.products[0])]);
  await dialog.getByRole('textbox', { name: 'Description', exact: true }).fill('Metadata changes preserve the selected products');
  await dialog.getByRole('button', { name: 'Save Changes', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  await row.getByRole('button', { name: `Edit ${value}`, exact: true }).click();
  await expect(page.getByRole('listbox', { name: 'Products', exact: true })).toHaveValues([String(fixture.products[0])]);
});

test('content sections and promotion rules save in their dialogs; media and builder retain their workflows', async ({ page }) => {
  await page.goto('./admin/content');
  await page.getByRole('button', { name: 'New Page', exact: true }).click();
  const title = name();
  await page.getByLabel('Title', { exact: true }).fill(title);
  await page.getByLabel('Section content 1', { exact: true }).fill('Content editor draft');
  await page.getByRole('button', { name: 'Add Section', exact: true }).click();
  await page.getByLabel('Section key 2', { exact: true }).fill('details');
  await page.getByLabel('Section content 2', { exact: true }).fill('Second content section');
  await page.getByRole('button', { name: 'Create Page', exact: true }).click();
  await expect(page.getByRole('dialog', { name: 'New Page', exact: true })).toHaveCount(0);
  const row = page.getByRole('row').filter({ hasText: title });
  await row.getByRole('button', { name: `Edit ${title}`, exact: true }).click();
  await expect(page.getByLabel('Section content 2', { exact: true })).toHaveValue('Second content section');
  await dismiss(page, 'cancel');
  await page.goto('./admin/promotions');
  await page.getByRole('button', { name: 'Create promotion', exact: true }).click();
  const code = 'DIALOG-' + Date.now();
  await page.getByLabel('Code', { exact: true }).fill(code);
  await page.getByLabel('Name', { exact: true }).fill('Dialog promotion');
  await page.getByRole('combobox', { name: 'Currency', exact: true }).selectOption('CHF');
  await page.getByLabel('Discount value', { exact: true }).fill('5');
  await page.getByLabel('Starts', { exact: true }).fill('2026-01-01T00:00');
  await dismiss(page, 'escape');
  await page.getByRole('button', { name: 'Continue Editing', exact: true }).click();
  await page.getByRole('button', { name: 'Create Promotion', exact: true }).click();
  await expect(page.getByRole('cell', { name: code, exact: true })).toBeVisible();
  await page.goto('./admin/media');
  await expect(page.getByRole('table', { name: 'Media library', exact: true })).toBeVisible();
  await expect(page.locator('dialog[open]')).toHaveCount(0);
  await page.goto('./admin/custom-builder');
  await expect(page.getByRole('heading', { name: 'Custom builder configuration', exact: true })).toBeVisible();
  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  await expect(page.getByRole('button', { name: 'Save draft', exact: true })).toBeInViewport();
});

for (const width of [1440, 768, 390]) {
  test(`product dialog at ${width}px keeps accessible actions, focus and fields within the viewport`, async ({ page }) => {
    await page.setViewportSize({ width, height: 900 });
    const dialog = await productDialog(page);
    await expect.poll(() => dialog.evaluate(element => element.scrollWidth <= element.clientWidth)).toBe(true);
    await dialog.locator('.admin-form-dialog-body').evaluate(body => { body.scrollTop = body.scrollHeight; });
    await expect(dialog.getByRole('button', { name: 'Create Product', exact: true })).toBeInViewport();
    await expect(dialog.getByRole('button', { name: /^Close / })).toBeInViewport();
    await dialog.getByRole('button', { name: 'Create Product', exact: true }).focus();
    await page.keyboard.press('Tab');
    await expect(dialog.getByRole('button', { name: /^Close / })).toBeFocused();
    if (width === 390) {
      await page.setViewportSize({ width, height: 450 });
      await expect(dialog.getByRole('button', { name: 'Create Product', exact: true })).toBeInViewport();
      await page.setViewportSize({ width, height: 900 });
    }
    const result = await new AxeBuilder({ page }).withTags(['wcag2a', 'wcag2aa', 'wcag21a', 'wcag21aa']).analyze();
    expect(result.violations.map(violation => ({ id: violation.id, targets: violation.nodes.map(node => node.target) }))).toEqual([]);
    await page.screenshot({ path: `../.runtime/admin-form-product-${width}.png` });
  });
}

test('route changes while nested confirmation is open release both scroll locks', async ({ page }) => {
  await page.goto('./admin/dashboard');
  await page.getByRole('link', { name: 'Products', exact: true }).click();
  await page.getByRole('button', { name: 'New Product', exact: true }).click();
  await page.getByLabel('Product name', { exact: true }).fill('A draft before navigating');
  await dismiss(page, 'escape');
  await expect(page.getByRole('dialog', { name: 'Discard changes?', exact: true })).toBeVisible();
  await page.goBack();
  await expect(page.getByRole('heading', { name: 'Dashboard', exact: true })).toBeVisible();
  await expect(page.locator('dialog[open]')).toHaveCount(0);
  await expect.poll(() => page.evaluate(() => document.body.style.overflow)).not.toBe('hidden');
});

for (const status of [404, 409]) {
  test(`editing retains product values when the server reports ${status}`, async ({ page }) => {
    const dialog = await productDialog(page);
    const value = await fillProduct(page);
    await dialog.getByRole('button', { name: 'Create Product', exact: true }).click();
    await expect(dialog).toHaveCount(0);
    await page.getByRole('button', { name: 'Find saved product', exact: true }).click();
    await page.getByRole('button', { name: `Edit ${value}`, exact: true }).click();
    const edit = page.getByRole('dialog', { name: 'Edit Product', exact: true });
    await expect(edit.getByRole('button', { name: 'Save Changes', exact: true })).toBeEnabled();
    await edit.getByLabel('Base price', { exact: true }).fill('39.25');
    await page.route('**/api/v1/admin/products/*', async route => {
      if (route.request().method() === 'PUT') return route.fulfill({ status, contentType: 'application/json', body: '{"title":"Record unavailable or changed"}' });
      return route.continue();
    });
    await edit.getByRole('button', { name: 'Save Changes', exact: true }).click();
    await expect(edit.getByRole('alert')).toContainText(status === 404 ? 'no longer available' : 'changed');
    await expect(edit.getByLabel('Product name', { exact: true })).toHaveValue(value);
    await expect(edit.getByLabel('Base price', { exact: true })).toHaveValue('39.25');
  });
}

test('category dismissal follows the same clean and unsaved policy for every close action', async ({ page }) => {
  await page.goto('./admin/categories');
  const trigger = page.getByRole('button', { name: 'New Category', exact: true });
  for (const method of ['backdrop', 'escape', 'cancel', 'close']) {
    await trigger.click();
    await dismiss(page, method);
    await expect(page.locator('dialog[open]')).toHaveCount(0);
    await expect(trigger).toBeFocused();
    await trigger.click();
    await page.getByLabel('Name', { exact: true }).fill('Keep category changes');
    await dismiss(page, method);
    const confirmation = page.getByRole('dialog', { name: 'Discard changes?', exact: true });
    await expect(confirmation).toBeVisible();
    await confirmation.getByRole('button', { name: 'Continue Editing', exact: true }).click();
    await expect(page.getByLabel('Name', { exact: true })).toHaveValue('Keep category changes');
    await dismiss(page, method);
    await confirmation.getByRole('button', { name: 'Discard Changes', exact: true }).click();
    await expect(page.locator('dialog[open]')).toHaveCount(0);
  }
});

test('collection editing retains all members when they span multiple API pages', async ({ page }) => {
  const headers = await authorization(page);
  const ids: number[] = [];
  for (let index = 0; index < 101; index++) {
    const response = await page.request.post(acceptanceApi + '/api/v1/admin/products', { headers, data: { name: name(), description: 'Collection pagination test', basePrice: 3, currency: 'CHF', stock: 1, isActive: true, collectionIds: [] } });
    expect(response.ok()).toBeTruthy(); ids.push((await response.json()).id);
  }
  const value = name();
  const collection = await page.request.post(acceptanceApi + '/api/v1/admin/collections', { headers, data: { name: value, isActive: true, sortOrder: 0 } });
  expect(collection.ok()).toBeTruthy();
  const id = (await collection.json()).id;
  const selected = await page.request.put(acceptanceApi + `/api/v1/admin/collections/${id}/products`, { headers, data: { productIds: ids } });
  expect(selected.ok()).toBeTruthy();
  await page.goto('./admin/collections');
  await page.getByRole('button', { name: `Edit ${value}`, exact: true }).click();
  const dialog = page.getByRole('dialog', { name: 'Edit Collection', exact: true });
  await expect(dialog.getByRole('button', { name: 'Save Changes', exact: true })).toBeEnabled();
  const membership = await dialog.getByRole('listbox', { name: 'Products', exact: true }).evaluate(select => Array.from((select as HTMLSelectElement).selectedOptions).map(option => option.value).sort());
  expect(membership).toEqual(ids.map(String).sort());
  await dialog.getByRole('textbox', { name: 'Description', exact: true }).fill('All 101 memberships are retained');
  await dialog.getByRole('button', { name: 'Save Changes', exact: true }).click();
  await expect(dialog).toHaveCount(0);
  const check = await page.request.get(acceptanceApi + `/api/v1/admin/products?collectionId=${id}&page=1&pageSize=100`, { headers });
  expect(check.ok()).toBeTruthy();
  const result = await check.json();
  expect(result.totalItems ?? result.totalCount).toBe(101);
});
