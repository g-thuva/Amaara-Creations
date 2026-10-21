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
  for (const width of [390, 768, 1440]) {
    await page.setViewportSize({ width, height: 1000 });
    for (const [name, route] of [['product', `products/${fixture.products[0]}`], ['profile', 'profile'], ['checkout', 'checkout'], ['help', 'help']]) {
      await page.goto(`http://127.0.0.1:5178/Amaara-Creations/${route}`);
      await page.locator('main h1').waitFor();
      await page.waitForFunction(() => !document.querySelector('[aria-label="Loading content"]'));
      // Only the disposable fixture email is masked; no real customer data is read.
      if (name === 'profile') await page.locator('input[type=email]').evaluate(input => { input.value = 'capture@example.test'; });
      await page.screenshot({ path: `${directory}/${name}-${width}.png`, fullPage: true });
      notes.push({ route: name, width, overflow: await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), data: 'disposable acceptance fixture' });
    }
  }
  const code = (await (await context.request.get(api + '/__fixture/authenticator-code')).json()).code;
  const staff = await context.request.post(api + '/api/v1/auth/login', { data: { email: fixture.adminEmail, password: fixture.password, authenticatorCode: code } });
  if (!staff.ok()) throw new Error('Disposable staff capture login failed');
  for (const width of [390, 768, 1440]) {
    await page.setViewportSize({ width, height: 1000 });
    await page.goto('http://127.0.0.1:5179/Amaara-Creations/admin/inventory');
    await page.getByRole('heading', { name: 'Inventory ledger', exact: true }).waitFor();
    await page.screenshot({ path: `${directory}/staff-${width}.png`, fullPage: true });
    notes.push({ route: 'staff', width, overflow: await page.evaluate(() => document.documentElement.scrollWidth > innerWidth), data: 'disposable acceptance fixture' });
  }
} finally {
  await writeFile(`${directory}/journeys.json`, JSON.stringify(notes, null, 2));
  await browser.close();
}
console.log('Captured product, profile, checkout, help and separate staff layouts.');
