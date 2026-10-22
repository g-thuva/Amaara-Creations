import { fillAuthenticator } from '../staffAuth';
import AxeBuilder from "@axe-core/playwright";
import { test, expect, type Page } from "@playwright/test";

async function openAdmin(
  page: Page,
  request: import("@playwright/test").APIRequestContext,
) {
  const response = await request.post(
    "http://127.0.0.1:5198/__fixture/operations",
  );
  expect(response.ok()).toBeTruthy();
  const fixture = await response.json();
  await page.goto("./login");
  await page
    .getByLabel("Email Address", { exact: true })
    .fill(fixture.superAdminEmail);
  await page
    .getByLabel("Password", { exact: true })
    .fill(fixture.basic.password);
  await fillAuthenticator(page); await page.getByRole("button", { name: "Sign In", exact: true }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  return fixture;
}

test("extracted product form, variants, media and archive actions persist through the real APIs", async ({
  page,
  request,
}) => {
  const fixture = await openAdmin(page, request);
  let productId: number | undefined;
  try {
    await page.goto("./admin/products");
    const name = "TEST architecture product " + Date.now();
    await page.getByLabel("Search products", { exact: true }).fill(name);
    await page.getByLabel("Name", { exact: true }).fill(name);
    await page
      .getByRole("combobox", { name: "Currency", exact: true })
      .selectOption("LKR");
    await page.getByLabel("Base price", { exact: true }).fill("100");
    await page.getByLabel("Stock", { exact: true }).fill("12");
    await page
      .getByLabel("SKU", { exact: true })
      .fill("TEST-ARCH-" + Date.now());
    await page
      .getByLabel("Description", { exact: true })
      .fill("Test-only catalog migration coverage.");
    const created = page.waitForResponse(
      (response) =>
        response.request().method() === "POST" &&
        response.url().endsWith("/admin/products"),
    );
    await page
      .getByRole("button", { name: "Create Product", exact: true })
      .click();
    const response = await created;
    expect(response.ok()).toBeTruthy();
    productId = (await response.json()).id;
    const row = page.getByRole("row").filter({ hasText: name });
    await expect(row).toBeVisible();
    await row.getByRole("button", { name: "Edit", exact: true }).click();
    await expect(page.getByLabel("Stock", { exact: true })).toBeDisabled();
    await expect(
      page.getByRole("combobox", { name: "Currency", exact: true }),
    ).toBeDisabled();
    await page.getByLabel("Base price", { exact: true }).fill("125");
    await page
      .getByRole("button", { name: "Save Product", exact: true })
      .click();
    await expect(row).toContainText("125.00");
    await row
      .getByRole("button", { name: "Media and variants", exact: true })
      .click();
    await page
      .getByLabel("Variant SKU", { exact: true })
      .fill("TEST-VARIANT-" + Date.now());
    await page.getByLabel("Variant name", { exact: true }).fill("TEST variant");
    await page.getByLabel("Variant stock", { exact: true }).fill("3");
    await page
      .getByRole("button", { name: "Add Variant", exact: true })
      .click();
    await expect(
      page.getByRole("table", { name: "Product variants", exact: true }),
    ).toContainText("TEST variant");
    await page.getByLabel("Product image", { exact: true }).setInputFiles({
      name: "test.png",
      mimeType: "image/png",
      buffer: Buffer.from(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+A8AAQUBAScY42YAAAAASUVORK5CYII=",
        "base64",
      ),
    });
    await page
      .getByLabel("Alt text", { exact: true })
      .fill("TEST product image");
    await page.getByRole("button", { name: "Upload", exact: true }).click();
    await expect(
      page.getByRole("table", { name: "Product images", exact: true }),
    ).toContainText("TEST product image");
    const accessibility = await new AxeBuilder({ page })
      .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
      .analyze();
    expect(
      accessibility.violations.map((v) => ({
        id: v.id,
        targets: v.nodes.map((n) => n.target),
      })),
    ).toEqual([]);
    await row.getByRole("button", { name: "Archive", exact: true }).click();
    await expect(
      row.getByRole("button", { name: "Reactivate", exact: true }),
    ).toBeVisible();
    await row.getByRole("button", { name: "Reactivate", exact: true }).click();
    await expect(
      row.getByRole("button", { name: "Archive", exact: true }),
    ).toBeVisible();
    await page.reload();
    await page.getByLabel("Search products", { exact: true }).fill(name);
    await expect(row).toContainText("125.00");
  } finally {
    // The acceptance host shares a disposable catalog across browser tests.
    // Keep generated products out of later storefront result sets, even on failure.
    if (productId) {
      const login = await page.request.post("http://127.0.0.1:5198/api/v1/auth/staff/refresh");
      expect(login.ok()).toBeTruthy();
      const auth = await login.json();
      const archived = await request.post(
        `http://127.0.0.1:5198/api/v1/admin/products/${productId}/archive`,
        { headers: { Authorization: "Bearer " + auth.token } },
      );
      expect(archived.ok()).toBeTruthy();
    }
  }
});

test("catalog and content editors retain their form behavior and accessible labels", async ({
  page,
  request,
}) => {
  await openAdmin(page, request);
  // Fixture creation expects the seeded category to remain the only category.
  const name = "Acceptance fixtures";
  await page.goto("./admin/categories");
  const row = page.getByRole("row").filter({ hasText: name });
  await expect(row).toBeVisible();
  await row.getByRole("button", { name: `Edit ${name}`, exact: true }).click();
  const description = page.getByRole("textbox", {
    name: "Description",
    exact: true,
  });
  const originalDescription = await description.inputValue();
  try {
    await description.fill("TEST category edit");
    await page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(
      page.getByText("Changes saved.", { exact: true }),
    ).toBeVisible();
  } finally {
    await row
      .getByRole("button", { name: `Edit ${name}`, exact: true })
      .click();
    await description.fill(originalDescription);
    await page.getByRole("button", { name: "Save", exact: true }).click();
    await expect(
      page.getByText("Changes saved.", { exact: true }),
    ).toBeVisible();
  }
  for (const route of [
    "content",
    "custom-builder",
    "collections",
    "settings",
  ]) {
    await page.goto("./admin/" + route);
    await expect(page.locator(".admin-content h2").first()).toBeVisible();
    await expect(
      page.getByText("Loading configuration…", { exact: true }),
    ).toHaveCount(0);
    const result = await new AxeBuilder({ page })
      .withTags(["wcag2a", "wcag2aa", "wcag21a", "wcag21aa"])
      .analyze();
    expect(
      result.violations.map((v) => ({
        route,
        id: v.id,
        targets: v.nodes.map((n) => n.target),
      })),
    ).toEqual([]);
  }
  await page.setViewportSize({ width: 375, height: 900 });
  const toggle = page.getByRole("button", {
    name: "Toggle admin navigation",
    exact: true,
  });
  await toggle.click();
  const menu = page.getByRole("navigation", {
    name: "Administration",
    exact: true,
  });
  await expect(menu).toBeVisible();
  await menu.getByRole("link", { name: "Dashboard", exact: true }).focus();
  await page.keyboard.press("Escape");
  await expect(toggle).toBeFocused();
  await expect(toggle).toHaveAttribute("aria-expanded", "false");
});
