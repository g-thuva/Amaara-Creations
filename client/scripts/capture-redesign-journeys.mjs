import { chromium } from '@playwright/test';
import { mkdir, writeFile } from 'node:fs/promises';
import process from 'node:process';

const directory = '../docs/redesign/evidence/after';
await mkdir(directory, { recursive: true });
const browser = await chromium.launch({ channel: process.platform === 'win32' ? 'chrome' : undefined });
const context = await browser.newContext();
const page = await context.newPage();
const api = 'http://127.0.0.1:5198';
const notes = [];
try {
  const fixture = await (await context.request.post(api + '/__fixture/new')).json();
  const login = await context.request.post(api + '/api/v1/auth/login', { data: { email: fixture.customerEmail, password: fixture.password } });
  if (!login.ok()) throw new Error('Disposable capture login failed');
  const auth = await login.json();
  const cart = await context.request.post(api + '/api/v1/cart', { data: { productId: fixture.products[0], quantity: 2 }, headers: { Authorization: 'Bearer ' + auth.token } });
  if (!cart.ok()) throw new Error('Disposable cart setup failed');
  const quote = await context.request.post(api + '/api/v1/account/requests/quotes', { data: { subject: 'Disposable capture: wedding place cards', details: 'Acceptance fixture only. Please review the configured materials and quantity.', quantity: 100 }, headers: { Authorization: 'Bearer ' + auth.token } });
  if (!quote.ok()) throw new Error('Disposable request setup failed');
  for (const width of [390, 768, 1440]) {
    await page.setViewportSize({ width, height: 1000 });
    for (const [name, route] of [['product', `products/${fixture.products[0]}`], ['profile', 'profile'], ['checkout', 'checkout'], ['help', 'help'], ['security', 'account/security'], ['requests', 'account/requests'], ['cart', 'cart']]) {
      await page.goto(`http://127.0.0.1:5178/Amaara-Creations/${route}`);
      await page.locator('main h1').waitFor();
      await page.waitForFunction(() => !document.querySelector('[aria-label="Loading content"]'));
      if (name === 'security') await page.getByRole('button', { name: 'Download account data', exact: true }).waitFor();
      if (name === 'requests') await page.getByRole('heading', { name: 'Disposable capture: wedding place cards', exact: true }).waitFor();
      // Only the disposable fixture email is masked; no real customer data is read.
      if (name === 'profile') await page.locator('input[type=email]').evaluate(input => { input.value = 'capture@example.test'; });
      await page.screenshot({ path: `${directory}/${name}-${width}.png`, fullPage: true });
      notes.push({ route: name, width, overflow: await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), data: 'disposable acceptance fixture' });
      if (name === 'profile') {
        await page.getByLabel('Profile photo', { exact: true }).setInputFiles({ name: 'capture-pixels.png', mimeType: 'image/png', buffer: Buffer.from('iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=', 'base64') });
        const crop = page.getByRole('dialog', { name: 'Crop profile photo', exact: true });
        await crop.getByRole('button', { name: 'Save cropped photo', exact: true }).waitFor();
        await page.screenshot({ path: `${directory}/photo-crop-${width}.png`, fullPage: true });
        notes.push({ route: 'photo-crop', width, overflow: await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), data: 'disposable acceptance pixels; crop cancelled without upload' });
        await crop.getByRole('button', { name: 'Cancel', exact: true }).click();
      }
    }
  }
  const code = (await (await context.request.get(api + '/__fixture/authenticator-code')).json()).code;
  const staff = await context.request.post(api + '/api/v1/auth/staff/login', { data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code } });
  if (!staff.ok()) throw new Error('Disposable staff capture login failed');
  for (const width of [390, 768, 1440]) {
    await page.setViewportSize({ width, height: 1000 });
    for (const [name, route, heading] of [['staff', 'inventory', 'Inventory ledger'], ['staff-quotes', 'quote-requests', 'Quote requests']]) {
      await page.goto(`http://127.0.0.1:5179/Amaara-Creations/admin/${route}`);
      await page.getByRole('heading', { level: 1, name: heading, exact: true }).waitFor();
      if (name === 'staff-quotes') await page.getByRole('heading', { name: 'Disposable capture: wedding place cards', exact: true }).waitFor();
      await page.screenshot({ path: `${directory}/${name}-${width}.png`, fullPage: true });
      notes.push({ route: name, width, overflow: await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), data: 'disposable acceptance fixture' });
    }
  }
} finally {
  await writeFile(`${directory}/journeys.json`, JSON.stringify(notes, null, 2));
  await browser.close();
}
console.log('Captured product, profile, checkout, help, security, requests, cart and separate staff layouts.');
