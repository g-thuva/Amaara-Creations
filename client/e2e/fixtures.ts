import { test as base, expect } from '@playwright/test';

export const acceptanceApi = 'http://127.0.0.1:5198';
type BrowserFixture = { products: number[]; category: number; cmsSlug: string; customerEmail: string; adminEmail: string; password: string };
export async function localManagementNavigation(target: import('@playwright/test').BrowserContext | import('@playwright/test').Page) {
  // Approved HTTPS deployment destination represented by the real local artifact.
  await target.route('https://management.example.test/**', route => {
    const destination = new URL(route.request().url());
    return route.fulfill({ status: 302, headers: { Location: 'http://127.0.0.1:5179' + destination.pathname + destination.search } });
  });
}
export const test = base.extend<{ fixture: BrowserFixture }>({
  context: async ({ context }, use) => {
    // HTTPS deployment navigation is a local test stand-in. Operational pages
    // still run the actual management artifact/API on the isolated loopback host.
    await localManagementNavigation(context);
    await use(context);
  },
  fixture: async ({ request }, use) => {
    const response = await request.post(`${acceptanceApi}/__fixture/new`);
    expect(response.ok(), 'Disposable test-only fixture must be available').toBeTruthy();
    await use(await response.json());
  },
});
export { expect };
export type { Page, APIRequestContext } from '@playwright/test';
