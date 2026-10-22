import { fillAuthenticator } from '../staffAuth';
import { test, expect, localManagementNavigation } from "../fixtures";
const api = "http://127.0.0.1:5198";
async function fixture(request: import("@playwright/test").APIRequestContext) {
  const response = await request.post(api + "/__fixture/operations");
  expect(response.ok()).toBeTruthy();
  return response.json();
}
async function login(
  page: import("@playwright/test").Page,
  email: string,
  password: string,
) {
  await localManagementNavigation(page);
  await page.goto(`http://127.0.0.1:${email.startsWith("acceptance-customer-") ? 5178 : 5179}/Amaara-Creations/login`);
  await page.getByLabel("Email Address", { exact: true }).fill(email);
  await page.getByLabel("Password", { exact: true }).fill(password);
  if (!email.startsWith('acceptance-customer-')) await fillAuthenticator(page);
  await page.getByRole("button", { name: /^(Sign In|Verify and sign in)$/ }).click();
  await expect(page).not.toHaveURL(/\/login$/);
}

test("admin inventory adjustment requires confirmation and records a ledger movement", async ({
  page,
  request,
}) => {
  const f = await fixture(request);
  await login(page, f.basic.adminEmail, f.basic.password);
  await page.goto("./admin/inventory");
  await expect(
    page.getByRole("heading", { name: "Inventory ledger", exact: true }),
  ).toBeVisible();
  await page
    .getByRole("button", { name: "Adjust Storefront QA print 01", exact: true })
    .click();
  await page.getByLabel("Quantity change", { exact: true }).fill("3");
  await page.getByLabel("Reason", { exact: true }).fill("TEST cycle count");
  await page.getByRole("button", { name: "Review adjustment" }).click();
  const dialog = page.getByRole("dialog");
  await expect(dialog).toBeVisible();
  await page.keyboard.press("Escape");
  await expect(
    page.getByRole("button", { name: "Review adjustment" }),
  ).toBeFocused();
  await page.getByRole("button", { name: "Review adjustment" }).click();
  await dialog.getByRole("button", { name: "Confirm", exact: true }).click();
  await expect(
    page.getByText("Inventory adjustment recorded.", { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByRole("cell", { name: "TEST cycle count", exact: true }),
  ).toBeVisible();
});

test("support sees customer/order operations while financial and security endpoints deny access", async ({
  page,
  request,
}) => {
  const f = await fixture(request);
  await login(page, f.supportEmail, f.basic.password);
  await page.goto(`./admin/customers/${f.customerId}`);
  await expect(
    page.getByRole("heading", { name: "Customer details", exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText("Email: Verified", { exact: false }),
  ).toBeVisible();
  await page.goto(`./admin/orders/${f.basic.orderId}`);
  await expect(
    page.getByRole("heading", { name: "Order snapshot" }),
  ).toBeVisible();
  await expect(
    page.getByRole("button", { name: "Reconcile payment", exact: true }),
  ).toHaveCount(0);
  await expect(
    page.getByRole("link", { name: "Staff", exact: true }),
  ).toHaveCount(0);
  const token = await page.request.post(api + "/api/v1/auth/staff/refresh");
  expect(token.ok()).toBeTruthy();
  const auth = await token.json();
  for (const path of ["staff", "reports", "audit", "payments", "settings"])
    expect(
      (
        await request.get(api + "/api/v1/admin/" + path, {
          headers: { Authorization: "Bearer " + auth.token },
        })
      ).status(),
    ).toBe(403);
  await page.goto("./admin/payments");
  await expect(
    page.getByRole("heading", { name: "Access denied", exact: true }),
  ).toBeVisible();
});

test("marketing creates a disabled coupon, edits it and opens redemption history", async ({
  page,
  request,
}) => {
  const f = await fixture(request);
  await login(page, f.marketingEmail, f.basic.password);
  await page.goto("./admin/promotions");
  await page.getByRole("button", { name: "Create promotion" }).click();
  const code = "TEST-" + Date.now();
  await page.getByLabel("Code", { exact: true }).fill(code);
  await page.getByLabel("Name", { exact: true }).fill("TEST campaign");
  await page
    .getByRole("combobox", { name: "Currency", exact: true })
    .selectOption("CHF");
  await page.getByLabel("Discount value", { exact: true }).fill("5");
  await page.getByLabel("Starts", { exact: true }).fill("2026-01-01T00:00");
  await page.getByRole("button", { name: "Save promotion" }).click();
  await expect(
    page.getByRole("cell", { name: code, exact: true }),
  ).toBeVisible();
  await page.getByRole("button", { name: `Edit ${code}`, exact: true }).click();
  await page.getByLabel("Name", { exact: true }).fill("TEST campaign edited");
  await page.getByRole("button", { name: "Save promotion" }).click();
  await expect(
    page.getByRole("cell", { name: "TEST campaign edited", exact: true }),
  ).toBeVisible();
  await page
    .getByRole("button", { name: `History ${code}`, exact: true })
    .click();
  await expect(
    page.getByRole("table", {
      name: "Promotion redemption history (latest 50)",
    }),
  ).toBeVisible();
});

test("proof approval unlocks validated production and shipment progress reaches the customer", async ({
  browser,
  request,
}) => {
  const f = await fixture(request);
  const admin = await browser.newPage();
  const customer = await browser.newPage();
  try {
    await login(admin, f.basic.adminEmail, f.basic.password);
    await admin.goto(
      `http://127.0.0.1:5179/Amaara-Creations/admin/custom-designs/${f.designId}`,
    );
    await expect(
      admin.getByText("No transition available.", { exact: false }),
    ).toBeVisible();
    await admin
      .getByLabel("Proof file", { exact: true })
      .setInputFiles({
        name: "TEST-proof.png",
        mimeType: "image/png",
        buffer: Buffer.from(
          "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=",
          "base64",
        ),
      });
    await admin
      .getByRole("button", { name: "Upload new proof revision" })
      .click();
    await expect(
      admin.getByText("Proof revision created.", { exact: true }),
    ).toBeVisible();
    await login(customer, f.basic.customerEmail, f.basic.password);
    await customer.goto(
      `http://127.0.0.1:5178/Amaara-Creations/account/designs/${f.designId}/proof`,
    );
    await customer.getByRole("button", { name: /^Approve revision / }).click();
    await expect(
      customer.getByText("Proof approved.", { exact: false }),
    ).toBeVisible();
    await admin.reload();
    for (const state of [
      "Queued",
      "Printing",
      "Finishing",
      "QualityCheck",
      "Ready",
    ]) {
      await admin
        .getByRole("combobox", { name: "Next production state", exact: true })
        .selectOption(state);
      await admin
        .getByLabel("Production note", { exact: true })
        .fill("TEST production " + state);
      await admin
        .getByRole("button", { name: "Update production", exact: true })
        .click();
      await expect(
        admin.getByText(`Production: ${state}`, { exact: false }),
      ).toBeVisible();
    }
    await admin.goto(
      `http://127.0.0.1:5179/Amaara-Creations/admin/orders/${f.basic.orderId}`,
    );
    for (const state of ["Packed", "Shipped", "Delivered"]) {
      await admin
        .getByRole("combobox", { name: "Next fulfilment status", exact: true })
        .selectOption(state);
      await admin
        .getByLabel("Carrier", { exact: true })
        .fill("TEST LOCAL CARRIER");
      await admin
        .getByLabel("Tracking number", { exact: true })
        .fill("TEST-123");
      await admin
        .getByRole("button", { name: "Update fulfilment", exact: true })
        .click();
      await expect(
        admin.getByText(`Fulfilment: ${state}`, { exact: false }),
      ).toBeVisible();
    }
    await customer.goto(
      `http://127.0.0.1:5178/Amaara-Creations/orders/${f.basic.orderId}`,
    );
    await expect(
      customer.getByText("TEST-123", { exact: false }),
    ).toBeVisible();
  } finally {
    await admin.close();
    await customer.close();
  }
});

test("analyst exports reports while Super Admin can review audit and staff", async ({
  page,
  request,
}) => {
  const f = await fixture(request);
  await login(page, f.analystEmail, f.basic.password);
  await page.goto("./admin/reports");
  await expect(
    page.getByRole("table", {
      name: "Settled funds by currency and environment",
    }),
  ).toBeVisible();
  const download = page.waitForEvent("download");
  await page.getByRole("button", { name: "Export orders CSV" }).click();
  expect((await download).suggestedFilename()).toBe("orders.csv");
  await page.getByRole("button", { name: "Logout", exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Welcome back', exact: true })).toBeVisible();
  await login(page, f.superAdminEmail, f.basic.password);
  await page.goto("./admin/audit");
  await expect(
    page.getByRole("table", { name: "Append-only audit evidence" }),
  ).toBeVisible();
  await page.goto("./admin/staff");
  await expect(
    page.getByRole("heading", { name: "Staff permissions" }),
  ).toBeVisible();
});

test("review moderation publishes and hides a pending review with a recorded reason", async ({
  page,
  request,
}) => {
  const f = await fixture(request);
  await login(page, f.basic.adminEmail, f.basic.password);
  await page.goto("./admin/reviews");
  const comment = "TEST pending moderation " + f.basic.orderId;
  const row = page
    .getByRole("row")
    .filter({ has: page.getByRole("cell", { name: comment, exact: true }) });
  await row
    .getByRole("combobox", { name: "New state", exact: true })
    .selectOption("Published");
  await row
    .getByLabel("Reason", { exact: true })
    .fill("TEST publication review");
  await row
    .getByRole("button", {
      name: `Review moderation for ${f.reviewId}`,
      exact: true,
    })
    .click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Confirm", exact: true })
    .click();
  await expect(
    row.getByRole("cell", { name: "Published", exact: true }),
  ).toBeVisible();
  expect(
    await (
      await request.get(api + `/api/v1/products/${f.basic.products[0]}/reviews`)
    ).text(),
  ).toContain(comment);
  await row
    .getByRole("combobox", { name: "New state", exact: true })
    .selectOption("Hidden");
  await row
    .getByLabel("Reason", { exact: true })
    .fill("TEST support follow-up");
  await row
    .getByRole("button", {
      name: `Review moderation for ${f.reviewId}`,
      exact: true,
    })
    .click();
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Confirm", exact: true })
    .click();
  await expect(
    row.getByRole("cell", { name: "TEST support follow-up", exact: true }),
  ).toBeVisible();
  expect(
    await (
      await request.get(api + `/api/v1/products/${f.basic.products[0]}/reviews`)
    ).text(),
  ).not.toContain(comment);
});
