import { test, expect, acceptanceApi } from '../fixtures';
import { readFile } from 'node:fs/promises';

test('saved artwork layout restores and exports the selected shape with transparent cutout', async ({ page, fixture }) => {
  expect((await page.request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } })).ok()).toBeTruthy();
  await page.goto('./custom');
  await expect(page.getByText('Custom item subtotal', { exact: true })).toBeVisible();
  await page.getByRole('combobox', { name: 'Shape', exact: true }).selectOption('round');
  await page.getByLabel(/^Width/).fill('8');
  const image = await page.evaluate(() => { const canvas = document.createElement('canvas'); canvas.width = 20; canvas.height = 10; const context = canvas.getContext('2d')!; context.fillStyle = '#ff0000'; context.fillRect(0, 0, 20, 10); return canvas.toDataURL('image/png').split(',')[1]; });
  await page.getByLabel('Artwork (JPEG, PNG or WebP)', { exact: true }).setInputFiles({ name: 'layout.png', mimeType: 'image/png', buffer: Buffer.from(image, 'base64') });
  await expect(page.getByText('layout.png', { exact: true })).toBeVisible();
  await page.getByText('Adjust text and artwork', { exact: true }).click();
  await page.getByRole('combobox', { name: 'Preview background', exact: true }).selectOption('transparent');
  await page.getByRole('combobox', { name: 'Artwork fit', exact: true }).selectOption('cover');
  await page.getByRole('slider', { name: 'Artwork zoom', exact: true }).fill('1.5');
  await page.getByRole('slider', { name: 'Artwork rotation', exact: true }).fill('25');
  await page.getByRole('slider', { name: 'Text horizontal position', exact: true }).fill('33');
  await page.getByRole('button', { name: 'Save design', exact: true }).click();
  await expect(page.getByText('Custom design saved.', { exact: true })).toBeVisible();
  await page.reload();
  await expect(page.getByAltText('Selected artwork preview')).toBeVisible();
  await page.getByText('Adjust text and artwork', { exact: true }).click();
  await expect(page.getByRole('combobox', { name: 'Preview background', exact: true })).toHaveValue('transparent');
  await expect(page.getByRole('slider', { name: 'Artwork zoom', exact: true })).toHaveValue('1.5');
  await expect(page.getByRole('slider', { name: 'Artwork rotation', exact: true })).toHaveValue('25');
  const downloaded = page.waitForEvent('download');
  await page.getByRole('button', { name: 'Download layout preview', exact: true }).click();
  const download = await downloaded;
  expect(download.suggestedFilename()).toMatch(/-layout-preview\.png$/);
  const bytes = await readFile((await download.path())!);
  expect(bytes.readUInt32BE(16)).toBe(945);
  expect(bytes.readUInt32BE(20)).toBe(945);
  const pixels = await page.evaluate(async base64 => { const image = new Image(); image.src = `data:image/png;base64,${base64}`; await image.decode(); const canvas = document.createElement('canvas'); canvas.width = image.width; canvas.height = image.height; const context = canvas.getContext('2d')!; context.drawImage(image, 0, 0); return { corner: Array.from(context.getImageData(0, 0, 1, 1).data), center: Array.from(context.getImageData(472, 472, 1, 1).data) }; }, bytes.toString('base64'));
  expect(pixels.corner[3]).toBe(0);
  expect(pixels.center).toEqual([255, 0, 0, 255]);
});

test('unsaved account-layout edits survive reload and a server conflict preserves local text', async ({ page, fixture }) => {
  expect((await page.request.post(`${acceptanceApi}/api/v1/auth/login`, { data: { email: fixture.customerEmail, password: fixture.password } })).ok()).toBeTruthy();
  await page.goto('./custom');
  await expect(page.getByText('Custom item subtotal', { exact: true })).toBeVisible();
  await page.getByRole('button', { name: 'Save design', exact: true }).click();
  await expect(page).toHaveURL(/\/custom\/\d+$/);
  await page.getByLabel('Sticker text (optional)', { exact: true }).fill('Preserve these local edits');
  await expect(page.getByRole('status').filter({ hasText: 'Draft saved for this browser session' })).toBeVisible();
  await page.reload();
  await expect(page.getByLabel('Sticker text (optional)', { exact: true })).toHaveValue('Preserve these local edits');
  let reject = true;
  await page.route('**/api/v1/custom-designs/*', async route => {
    if (route.request().method() === 'PUT' && reject) { reject = false; return route.fulfill({ status: 409, contentType: 'application/json', body: JSON.stringify({ message: 'Design changed' }) }); }
    return route.continue();
  });
  await page.getByRole('button', { name: 'Save design', exact: true }).click();
  await expect(page.getByRole('region', { name: 'Conflicting design edits' })).toBeVisible();
  await page.getByRole('button', { name: 'Recover local edits', exact: true }).click();
  await expect(page.getByLabel('Sticker text (optional)', { exact: true })).toHaveValue('Preserve these local edits');
  await page.getByRole('button', { name: 'Save design', exact: true }).click();
  await page.reload();
  await expect(page.getByLabel('Sticker text (optional)', { exact: true })).toHaveValue('Preserve these local edits');
});
