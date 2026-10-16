import { test, expect, acceptanceApi } from '../fixtures';

test('a malformed browser draft cannot block the custom builder', async ({ page }) => {
  await page.addInitScript(() => sessionStorage.setItem('amaara-custom-builder-draft', '{broken'));
  await page.goto('./custom');
  await expect(page.getByRole('heading', { name: 'Build a custom sticker', exact: true })).toBeVisible();
  await expect(page.getByText('Custom item subtotal', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Save design', exact: true })).toBeEnabled();
});

test('changing a design invalidates its displayed quote before the next quote completes', async ({ page }) => {
  await page.goto('./custom');
  await expect(page.getByText('Custom item subtotal', { exact: true })).toBeVisible();
  let received = false;
  let release!: () => void;
  const gate = new Promise<void>(resolve => { release = resolve; });
  await page.route('**/api/v1/custom-builder/quote', async route => {
    const response = await route.fetch();
    received = true;
    await gate;
    await route.fulfill({ response });
  });
  const width = page.getByLabel(/^Width/);
  const initial = Number(await width.inputValue());
  const step = Number(await width.getAttribute('step')) || 1;
  const max = Number(await width.getAttribute('max'));
  await width.fill(String(initial + step <= max ? initial + step : initial - step));
  try {
    await expect(page.getByText('Custom item subtotal', { exact: true })).toHaveCount(0);
    await expect(page.getByRole('button', { name: 'Save design', exact: true })).toBeDisabled();
    await expect.poll(() => received).toBe(true);
  } finally { release(); }
  await expect(page.getByText('Custom item subtotal', { exact: true })).toBeVisible();
  await expect(page.getByRole('button', { name: 'Save design', exact: true })).toBeEnabled();
});

test('two synchronous save clicks create one persisted design', async ({ page, context, fixture }) => {
  const response = await context.request.post(`${acceptanceApi}/api/v1/auth/login`, {
    data: { email: fixture.customerEmail, password: fixture.password },
  });
  expect(response.ok()).toBe(true);
  let creates = 0;
  await page.route('**/api/v1/custom-designs', async route => {
    if (route.request().method() === 'POST') creates++;
    await route.continue();
  });
  await page.goto('./custom');
  const save = page.getByRole('button', { name: 'Save design', exact: true });
  await expect(save).toBeEnabled();
  await save.evaluate(button => { (button as HTMLButtonElement).click(); (button as HTMLButtonElement).click(); });
  await expect(page).toHaveURL(/\/custom\/\d+$/);
  await expect(page.getByText('Custom design saved.', { exact: true })).toBeVisible();
  expect(creates).toBe(1);
});
