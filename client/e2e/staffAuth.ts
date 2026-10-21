import type { Page } from '@playwright/test';

// Real TOTP verification with a key belonging only to disposable test accounts.
export async function fillAuthenticator(page: Page) {
  const field = page.getByLabel('Authenticator code', { exact: true });
  if (!(await field.count())) return;
  const response = await page.request.get('http://127.0.0.1:5198/__fixture/authenticator-code');
  if (!response.ok()) throw new Error('Disposable TOTP fixture is unavailable');
  await field.fill((await response.json()).code);
}
