import { test } from '../fixtures';
import { checkAccessibility as check } from '../accessibility';
test('public catalog, builder and account entry forms pass automated WCAG rules', async ({ page, fixture }) => {
  test.setTimeout(180000); await page.emulateMedia({ reducedMotion: 'reduce' });
  for (const route of ['', 'products', `products/${fixture.products[0]}`, 'custom', 'login', 'register', 'forgot-password']) await check(page, route);
});
