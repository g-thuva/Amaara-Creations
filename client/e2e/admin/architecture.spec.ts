import { fillAuthenticator } from '../staffAuth';
import { test, expect, type Page } from "../fixtures";

const api = "http://127.0.0.1:5198";
async function login(page: Page, email: string, password: string) {
  await page.goto("./login");
  await page.getByLabel("Email Address", { exact: true }).fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  await fillAuthenticator(page); await page.getByRole("button", { name: /^(Sign In|Verify and sign in)$/ }).click();
  await expect(page).not.toHaveURL(/\/login$/);
}

test("inventory movement pagination is independent and resets for a different product", async ({
  page,
  request,
}) => {
  const fixtureResponse = await request.post(api + "/__fixture/operations");
  expect(fixtureResponse.ok()).toBeTruthy();
  const fixture = await fixtureResponse.json();
  await login(page, fixture.basic.adminEmail, fixture.basic.password);
  // Controlled list windows exercise the UI contract without generating 51 stock writes.
  const movementQueries: string[] = [];
  const balanceQueries: string[] = [];
  await page.route("**/api/v1/admin/inventory?*", async (route) => {
    const url = new URL(route.request().url());
    balanceQueries.push(url.search);
    const pageNumber = Number(url.searchParams.get("page") || 1);
    await route.fulfill({
      json: {
        items: [
          {
            productId: pageNumber,
            variantId: null,
            product: "TEST product " + pageNumber,
            variant: "Base stock",
            sku: "TEST-" + pageNumber,
            available: 20,
            reserved: 0,
            onHand: 20,
            lowStockThreshold: 1,
          },
        ],
        page: pageNumber,
        pageSize: 20,
        totalCount: 21,
      },
    });
  });
  await page.route("**/api/v1/admin/inventory/movements?*", async (route) => {
    const url = new URL(route.request().url());
    movementQueries.push(url.search);
    await route.fulfill({
      json: {
        items: [
          {
            id: Number(url.searchParams.get("page")),
            movementType: "Adjustment",
            quantityDelta: 1,
            newQuantity: 20,
            reason: "TEST movement page " + url.searchParams.get("page"),
            createdAt: "2026-01-01T00:00:00Z",
          },
        ],
        totalCount: 51,
      },
    });
  });
  await page.goto("./admin/inventory");
  const balances = page.getByRole("navigation", {
    name: "Stock balance pages",
    exact: true,
  });
  const movements = page.getByRole("navigation", {
    name: "Inventory movement pages",
    exact: true,
  });
  await expect(movements).toContainText("Page 1 of 2");
  const initialBalanceRequests = balanceQueries.length;
  await movements.getByRole("button", { name: "Next", exact: true }).click();
  await expect(movements).toContainText("Page 2 of 2");
  await expect(balances).toContainText("Page 1 of 2");
  expect(balanceQueries).toHaveLength(initialBalanceRequests);
  await expect(
    page.getByRole("cell", { name: "TEST movement page 2", exact: true }),
  ).toBeVisible();
  const initialMovementRequests = movementQueries.length;
  await balances.getByRole("button", { name: "Next", exact: true }).click();
  await expect(balances).toContainText("Page 2 of 2");
  await expect(movements).toContainText("Page 2 of 2");
  expect(movementQueries).toHaveLength(initialMovementRequests);
  await page
    .getByRole("button", { name: "Adjust TEST-2", exact: true })
    .click();
  await expect(movements).toContainText("Page 1 of 2");
  await expect.poll(() => movementQueries.at(-1)).toContain("productId=2");
  expect(new URLSearchParams(movementQueries.at(-1)).get("page")).toBe("1");
});

async function harness(page: Page, code: string) {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await page.route("**/__admin_architecture_test", (route) =>
    route.fulfill({
      contentType: "text/html",
      body: `
    <div id="root"></div><script type="module">
    import React from '/Amaara-Creations/node_modules/.vite/deps/react.js';
    import ReactDOM from '/Amaara-Creations/node_modules/.vite/deps/react-dom_client.js';
    const h = React.createElement;
    ${code}
    ReactDOM.createRoot(document.getElementById('root')).render(h(Harness));
    </script>`,
    }),
  );
  await page.goto("./__admin_architecture_test");
  return errors;
}

test("product hook rejects older filter results even when the loader ignores cancellation", async ({
  page,
}) => {
  const errors = await harness(
    page,
    `
    import { useProducts } from '/Amaara-Creations/src/features/admin/features/products/useProducts.js';
    import { productsApi } from '/Amaara-Creations/src/features/admin/features/products/productsApi.js';
    import { categoriesApi } from '/Amaara-Creations/src/features/admin/features/categories/categoriesApi.js';
    import { collectionsApi } from '/Amaara-Creations/src/features/admin/features/collections/collectionsApi.js';
    const requests = [];
    productsApi.getProducts = () => new Promise(resolve => requests.push(resolve));
    categoriesApi.getCategories = async () => [];
    collectionsApi.getCollections = async () => [];
    function Harness() {
      const { query, setQuery, resource } = useProducts();
      return h('main', {},
        h('p', { role: 'status' }, resource.loading ? 'Loading' : 'Ready'),
        h('pre', { 'aria-label': 'Product results' }, JSON.stringify(resource.data)),
        h('button', { onClick: () => setQuery({ ...query, search: 'older' }) }, 'Older filter'),
        h('button', { onClick: () => setQuery({ ...query, search: 'newer' }) }, 'Newer filter'),
        h('button', { onClick: () => requests[0]({ items: [{ name: 'Initial' }] }) }, 'Initial result'),
        h('button', { onClick: () => requests[1]({ items: [{ name: 'Stale' }] }) }, 'Older result'),
        h('button', { onClick: () => requests[2]({ items: [{ name: 'Latest' }] }) }, 'Newer result'));
    }
  `,
  );
  await page
    .getByRole("button", { name: "Initial result", exact: true })
    .click();
  await expect(page.getByRole("status")).toHaveText("Ready");
  await page.getByRole("button", { name: "Older filter", exact: true }).click();
  await page.getByRole("button", { name: "Newer filter", exact: true }).click();
  await page.getByRole("button", { name: "Newer result", exact: true }).click();
  await expect(page.getByLabel("Product results")).toContainText("Latest");
  await page.getByRole("button", { name: "Older result", exact: true }).click();
  await expect(page.getByLabel("Product results")).toContainText("Latest");
  expect(errors).toEqual([]);
});

test("admin mutation hook locks synchronous duplicate submissions and returns state only", async ({
  page,
}) => {
  const errors = await harness(
    page,
    `
    import { useAdminMutation } from '/Amaara-Creations/src/features/admin/shared/hooks/useAdminMutation.js';
    let calls = 0;
    let finish;
    function Harness() {
      const mutation = useAdminMutation();
      return h('main', {},
        h('p', { role: 'status' }, mutation.pending ? 'Pending' : mutation.success || 'Ready'),
        mutation.error && h('p', { role: 'alert' }, mutation.error),
        h('pre', { 'aria-label': 'Mutation interface' }, Object.keys(mutation).join(',')),
        h('button', { onClick: () => {
          mutation.run(() => { calls++; return new Promise(resolve => { finish = resolve; }); });
          mutation.run(() => { calls++; });
        } }, 'Submit twice'),
        h('button', { onClick: () => finish() }, 'Complete'),
        h('button', { onClick: () => mutation.run(() => Promise.reject({ response: { status: 409 } })) }, 'Reject'),
        h('button', { onClick: mutation.reset }, 'Reset'),
        h('output', {}, String(calls)));
    }
  `,
  );
  expect(
    await page.getByLabel("Mutation interface").textContent(),
  ).not.toContain("feedback");
  await page.getByRole("button", { name: "Submit twice", exact: true }).click();
  await expect(page.locator('p[role="status"]')).toHaveText("Pending");
  await expect(page.locator("output")).toHaveText("1");
  await page.getByRole("button", { name: "Complete", exact: true }).click();
  await expect(page.locator('p[role="status"]')).toHaveText("Changes saved.");
  await page.getByRole("button", { name: "Reject", exact: true }).click();
  await expect(page.getByRole("alert")).toContainText("This item changed.");
  await page.getByRole("button", { name: "Reset", exact: true }).click();
  await expect(page.getByRole("alert")).toHaveCount(0);
  expect(errors).toEqual([]);
});

test("every permitted admin navigation target survives direct entry and reload", async ({
  page,
  request,
}) => {
  test.setTimeout(180000);
  const response = await request.post(api + "/__fixture/operations");
  expect(response.ok()).toBeTruthy();
  const fixture = await response.json();
  await login(page, fixture.superAdminEmail, fixture.basic.password);
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await page.goto("./admin");
  await expect(page).toHaveURL(/\/admin\/dashboard$/);
  // The index redirect can briefly show its shell before the lazy dashboard suspends.
  // Wait for the actual destination before taking a non-retrying DOM snapshot.
  await expect(page.getByRole("heading", { name: "Dashboard", exact: true, level: 1 })).toBeVisible();
  const navigation = page.getByRole("navigation", {
    name: "Administration",
    exact: true,
  });
  await expect(navigation).toBeVisible();
  await expect(navigation.getByRole("link")).toHaveCount(21);
  const links = await navigation
    .getByRole("link")
    .evaluateAll((links) => links.map((link) => link.getAttribute("href")!));
  expect(links).toHaveLength(21);
  const headings: Record<string, string> = {
    "quote-requests": "Quote requests",
    "privacy-requests": "Privacy requests",
    payments: "Payments and refunds",
    inventory: "Inventory ledger",
    promotions: "Promotions",
    reports: "Reports",
    audit: "Admin audit log",
    staff: "Staff permissions",
    shipping: "Shipping zones and methods",
    dashboard: "Dashboard",
    products: "Product Management",
    categories: "Categories",
    collections: "Collections",
    media: "Media Library",
    content: "Content",
    settings: "Site Settings",
    "custom-builder": "Custom builder configuration",
    "custom-designs": "Custom design production",
    orders: "Order management",
    customers: "Customers",
    reviews: "Review moderation",
  };
  for (const href of links) {
    await page.goto(href);
    const expectedUrl = new URL(href, page.url()).href;
    const destination = new URL(expectedUrl).pathname.split("/").at(-1)!;
    expect(headings[destination], `Expected page heading for ${destination}`).toBeTruthy();
    const heading = page.locator(".admin-content").getByRole("heading", { name: headings[destination], exact: true });
    await expect(page).toHaveURL(expectedUrl);
    await expect(
      page.getByRole("heading", { name: "Access denied", exact: true }),
    ).toHaveCount(0);
    await expect(heading).toBeVisible();
    await page.reload();
    await expect(page).toHaveURL(expectedUrl);
    await expect(heading).toBeVisible();
    await expect(
      page.getByText("Something went wrong", { exact: false }),
    ).toHaveCount(0);
  }
  await expect(
    page.getByRole("heading", { name: "Review moderation", exact: true }),
  ).toBeVisible();
  expect(errors).toEqual([]);
});

test("malformed historical order and production snapshots preserve the surrounding detail page", async ({
  page,
  request,
}) => {
  const response = await request.post(api + "/__fixture/operations");
  expect(response.ok()).toBeTruthy();
  const fixture = await response.json();
  await login(page, fixture.basic.adminEmail, fixture.basic.password);
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  await page.route(
    `**/api/v1/admin/orders/${fixture.basic.orderId}`,
    async (route) => {
      const original = await route.fetch();
      const data = await original.json();
      data.orderItems[0].configurationSnapshotJson = "{invalid<script>";
      await route.fulfill({ response: original, json: data });
    },
  );
  await page.goto(`./admin/orders/${fixture.basic.orderId}`);
  await page.getByRole("article").first().locator("summary").first().click();
  await expect(
    page.getByText("This historical snapshot could not be read.", {
      exact: true,
    }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Order history", exact: true }),
  ).toBeVisible();
  await page.route(
    `**/api/v1/admin/custom-designs/${fixture.designId}`,
    async (route) => {
      const original = await route.fetch();
      const data = await original.json();
      data.order.configurationSnapshotJson = "{invalid<script>";
      await route.fulfill({ response: original, json: data });
    },
  );
  await page.goto(`./admin/custom-designs/${fixture.designId}`);
  await expect(
    page.getByText("This historical snapshot could not be read.", {
      exact: true,
    }),
  ).toBeVisible();
  await expect(
    page.getByRole("heading", { name: "Production transition", exact: true }),
  ).toBeVisible();
  expect(errors).toEqual([]);
});
