import { test as base, expect } from '@playwright/test';

export const acceptanceApi = 'http://127.0.0.1:5198';
type BrowserFixture = { products: number[]; category: number; cmsSlug: string; customerEmail: string; adminEmail: string; password: string };
export const test = base.extend<{ fixture: BrowserFixture }>({
  fixture: async ({ request }, use) => {
    const response = await request.post(`${acceptanceApi}/__fixture/new`);
    expect(response.ok(), 'Disposable test-only fixture must be available').toBeTruthy();
    await use(await response.json());
  },
});
export { expect };
