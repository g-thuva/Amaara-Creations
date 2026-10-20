import { chromium } from '@playwright/test';

const origin = new URL(process.argv[2]);
if (origin.protocol !== 'https:' || !['127.0.0.1', 'localhost'].includes(origin.hostname)) throw new Error('Published rehearsal browser accepts HTTPS loopback only.');
const browser = await chromium.launch({ ...(process.platform === 'win32' ? { channel: 'chrome' } : {}) });
let stage = 'public routes';
try {
  const context = await browser.newContext({ ignoreHTTPSErrors: true }); // Generated loopback certificate only.
  const page = await context.newPage();
  page.setDefaultTimeout(15000);
  const errors = [];
  page.on('pageerror', error => errors.push(error.name));
  await page.route('**/*', route => new URL(route.request().url()).origin === origin.origin ? route.continue() : route.abort());
  for (const path of ['/', '/products', `/products/${process.argv[3]}`, '/pages/release-information', '/custom']) {
    stage = path;
    await page.goto(new URL(path, origin).href);
    await page.locator('main h1, main h2, main [role="alert"]').first().waitFor();
  }
  await page.goto(new URL('/login', origin).href);
  stage = 'login';
  await page.getByLabel('Email Address', { exact: true }).fill(process.env.AMAARA_REHEARSAL_EMAIL);
  await page.getByLabel('Password', { exact: true }).fill(process.env.AMAARA_REHEARSAL_PASSWORD);
  await page.getByRole('button', { name: 'Sign In', exact: true }).click();
  await page.waitForURL(url => !url.pathname.endsWith('/login'));
  const session = (await context.cookies()).find(cookie => cookie.name === 'amaara_refresh');
  if (!session?.secure || !session.httpOnly || session.sameSite !== 'Lax') throw new Error('Published refresh cookie is missing required protections.');
  for (const path of ['/profile', '/cart', '/orders', '/admin/orders', '/admin/inventory', '/admin/reports']) {
    stage = path;
    await page.goto(new URL(path, origin).href);
    await page.locator('main h1, main h2').first().waitFor();
    if (page.url().endsWith('/login')) throw new Error('Published secure session did not survive reload.');
  }
  if (errors.length) throw new Error('Published artifact reported browser runtime errors.');
  console.log('Published browser smoke: PASS; public routes, CMS, builder availability, secure session, account/cart/orders and admin reads.');
} catch (error) {
  console.error(`Published browser smoke: FAIL; stage=${stage}; type=${error.name}`);
  process.exitCode = 1;
} finally {
  await browser.close();
}
