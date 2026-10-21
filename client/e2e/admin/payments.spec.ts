import { fillAuthenticator } from '../staffAuth';
import { type APIRequestContext, type Page } from "@playwright/test";
import { test, expect, acceptanceApi } from "../fixtures";

// Real Identity, refund service and detail DTOs in the disposable acceptance DB;
// only the payment gateway is fake. No Datatrans credentials or merchant money.
async function openRefundOrder(
  page: Page,
  request: APIRequestContext,
  outcome = "Completed",
) {
  const response = await request.post(`${acceptanceApi}/__fixture/refund`, {
    data: { outcome },
  });
  expect(response.ok()).toBeTruthy();
  const fixture = (await response.json()) as {
    orderId: number;
    paymentId: number;
    adminEmail: string;
    password: string;
  };
  await page.goto("./login");
  await page
    .getByLabel("Email Address", { exact: true })
    .fill(fixture.adminEmail);
  await page.getByLabel("Password", { exact: true }).fill(fixture.password);
  await fillAuthenticator(page); await page.getByRole("button", { name: "Sign In", exact: true }).click();
  await expect(page).not.toHaveURL(/\/login$/);
  await page.goto(`./admin/orders/${fixture.orderId}`);
  await expect(page.getByLabel("Refund amount")).toBeEnabled();
  return fixture;
}

async function submitRefund(
  page: Page,
  amount: number,
  reason = "Test-only browser refund",
  httpStatus = 200,
) {
  await page.getByLabel("Refund amount").fill(String(amount));
  await page.getByLabel("Refund reason").fill(reason);
  await page
    .getByRole("button", { name: "Review refund", exact: true })
    .click();
  const response = page.waitForResponse(
    (r) =>
      /\/admin\/payments\/\d+\/refunds$/.test(r.url()) &&
      r.request().method() === "POST",
  );
  await page
    .getByRole("dialog")
    .getByRole("button", { name: "Confirm", exact: true })
    .click();
  const received = await response;
  const safeException =
    received.status() === 500
      ? (await received.text())
          .split(/\r?\n/)
          .find((line) => line.startsWith("System."))
      : "";
  expect(received.status(), safeException).toBe(httpStatus);
  return received.json();
}

async function expectAmounts(
  page: Page,
  refunded: string,
  remaining: string,
  paymentStatus: string,
) {
  await expect(
    page.getByText(`Settled: LKR 225.00 · Refunded: LKR ${refunded}`, {
      exact: true,
    }),
  ).toBeVisible();
  await expect(
    page.getByText(`Remaining refundable: LKR ${remaining}`, { exact: true }),
  ).toBeVisible();
  await expect(
    page.getByText(`FakeTestGateway: ${paymentStatus} · LKR 225.00`, {
      exact: true,
    }),
  ).toBeVisible();
  await expect(
    page.getByText(
      new RegExp(`^Order: Confirmed · Payment: ${paymentStatus} ·`),
    ),
  ).toBeVisible();
}

test("completed partial refund updates immediately and stays correct after refetch and browser refresh", async ({
  page,
  request,
}) => {
  await openRefundOrder(page, request);
  const refund = await submitRefund(page, 50);
  expect(refund.status).toBe("Completed");
  await expect(
    page.getByRole("status").filter({ hasText: "Request status: Completed." }),
  ).toBeVisible();
  await expect(
    page.getByText("LKR 50.00 — Completed: Test-only browser refund", {
      exact: true,
    }),
  ).toBeVisible();
  await expectAmounts(page, "50.00", "175.00", "PartiallyRefunded");
  const refetched = Promise.all([
    page.waitForResponse(
      (r) =>
        /\/admin\/payments\/\d+$/.test(r.url()) &&
        r.request().method() === "GET",
    ),
    page.waitForResponse(
      (r) =>
        /\/admin\/orders\/\d+$/.test(r.url()) && r.request().method() === "GET",
    ),
  ]);
  await page
    .getByRole("button", { name: "Refresh payment details", exact: true })
    .click();
  for (const response of await refetched) expect(response.status()).toBe(200);
  await expect(
    page.getByText("Refreshing payment details…", { exact: true }),
  ).toHaveCount(0);
  await expectAmounts(page, "50.00", "175.00", "PartiallyRefunded");
  await expect(
    page.getByText("Request status: Completed.", { exact: true }),
  ).toBeVisible();
  await page.reload();
  await expectAmounts(page, "50.00", "175.00", "PartiallyRefunded");
  await expect(
    page.getByText("LKR 50.00 — Completed: Test-only browser refund", {
      exact: true,
    }),
  ).toBeVisible();
});

test("two sequential partial refunds with identical details are independent and update the balance", async ({
  page,
  request,
}) => {
  await openRefundOrder(page, request);
  const first = await submitRefund(page, 50, "Same test-only reason");
  await expectAmounts(page, "50.00", "175.00", "PartiallyRefunded");
  const second = await submitRefund(page, 50, "Same test-only reason");
  expect(second.id).not.toBe(first.id);
  expect(second.status).toBe("Completed");
  await expect(
    page.getByText("LKR 50.00 — Completed: Same test-only reason", {
      exact: true,
    }),
  ).toHaveCount(2);
  await expectAmounts(page, "100.00", "125.00", "PartiallyRefunded");
  await page.reload();
  await expectAmounts(page, "100.00", "125.00", "PartiallyRefunded");
});

test("full refund shows the authoritative Refunded state and zero remaining balance", async ({
  page,
  request,
}) => {
  await openRefundOrder(page, request);
  expect((await submitRefund(page, 225)).status).toBe("Completed");
  await expectAmounts(page, "225.00", "0.00", "Refunded");
  await expect(
    page.getByText("Request status: Completed.", { exact: true }),
  ).toBeVisible();
  await expect(page.getByLabel("Refund amount")).toHaveCount(0);
  await page.reload();
  await expectAmounts(page, "225.00", "0.00", "Refunded");
});

test("provider rejection returned as HTTP 200 shows Failed with no successful notification or balance change", async ({
  page,
  request,
}) => {
  await openRefundOrder(page, request, "Failed");
  expect((await submitRefund(page, 50)).status).toBe("Failed");
  await expect(
    page.getByRole("alert").filter({ hasText: "Request status: Failed." }),
  ).toBeVisible();
  await expect(
    page.getByText(
      "No refund was completed. Check the payment details and provider result before trying again.",
      { exact: true },
    ),
  ).toBeVisible();
  await expect(
    page.getByText("Request status: Completed.", { exact: true }),
  ).toHaveCount(0);
  await expect(
    page.getByText("LKR 50.00 — Failed: Test-only browser refund", {
      exact: true,
    }),
  ).toBeVisible();
  await expectAmounts(page, "0.00", "225.00", "Paid");
  await page.reload();
  await expectAmounts(page, "0.00", "225.00", "Paid");
});

test("ambiguous provider refund stays Pending and reserves funds without claiming completion", async ({
  page,
  request,
}) => {
  await openRefundOrder(page, request, "Pending");
  const pendingRefund = await submitRefund(page, 50);
  expect(pendingRefund.status).toBe("Pending");
  await expect(
    page.getByRole("status").filter({ hasText: "Request status: Pending." }),
  ).toBeVisible();
  await expect(
    page.getByText(/Awaiting provider verification\./),
  ).toBeVisible();
  await expect(
    page.getByText("Request status: Completed.", { exact: true }),
  ).toHaveCount(0);
  await expect(
    page
      .getByRole("listitem")
      .filter({ hasText: "LKR 50.00 — Pending: Test-only browser refund" }),
  ).toBeVisible();
  await expectAmounts(page, "0.00", "175.00", "Paid");
  await page
    .getByRole("button", { name: "Reconcile refund", exact: true })
    .click();
  await expect(
    page.getByText("Request status: Pending.", { exact: true }),
  ).toBeVisible();
  await expectAmounts(page, "0.00", "175.00", "Paid");
  const repeated = await submitRefund(page, 50);
  expect(repeated.id).toBe(pendingRefund.id);
  expect(repeated.status).toBe("Pending");
  await expect(
    page
      .getByRole("listitem")
      .filter({ hasText: "LKR 50.00 — Pending: Test-only browser refund" }),
  ).toHaveCount(1);
  await expectAmounts(page, "0.00", "175.00", "Paid");
  await page.reload();
  await expectAmounts(page, "0.00", "175.00", "Paid");
});

test("HTTP refund rejection exposes a safe error and never changes the amount or announces success", async ({
  page,
  request,
}) => {
  const fixture = await openRefundOrder(page, request);
  // Send an invalid amount through the actual controller to test ProblemDetails.
  await page.route(
    `**/admin/payments/${fixture.paymentId}/refunds`,
    async (route) => {
      const rejected = await route.fetch({
        postData: JSON.stringify({
          ...route.request().postDataJSON(),
          amount: 999,
        }),
      });
      await route.fulfill({ response: rejected });
    },
  );
  const rejected = await submitRefund(page, 50, "Rejected request", 409);
  expect(rejected.code).toBe("REFUND_EXCEEDS_AVAILABLE_AMOUNT");
  await expect(
    page
      .getByRole("alert")
      .filter({ hasText: "The refund exceeds the available settled amount." }),
  ).toBeVisible();
  await expect(
    page.getByText(
      "Refresh payment details and review refund history before trying again.",
      { exact: true },
    ),
  ).toBeVisible();
  await expect(
    page.getByText("Request status: Completed.", { exact: true }),
  ).toHaveCount(0);
  await expect(page.getByText("No refunds.", { exact: true })).toBeVisible();
  await expectAmounts(page, "0.00", "225.00", "Paid");
});

test("failed detail refresh preserves the refund result and blocks further credits until recovery", async ({
  page,
  request,
}) => {
  const fixture = await openRefundOrder(page, request);
  let afterRefund = false;
  page.on("response", (response) => {
    if (response.url().endsWith(`/admin/payments/${fixture.paymentId}/refunds`))
      afterRefund = true;
  });
  const failOnce = new Set([
    `/admin/payments/${fixture.paymentId}`,
    `/admin/orders/${fixture.orderId}`,
  ]);
  await page.route(
    /\/api\/v1\/admin\/(orders|payments)\/\d+$/,
    async (route) => {
      const path = new URL(route.request().url()).pathname.replace(
        "/api/v1",
        "",
      );
      if (afterRefund && failOnce.delete(path))
        await route.fulfill({
          status: 503,
          contentType: "application/problem+json",
          body: JSON.stringify({ title: "Test-only temporary outage" }),
        });
      else await route.continue();
    },
  );
  expect((await submitRefund(page, 50)).status).toBe("Completed");
  await expect(
    page.getByText("Request status: Completed.", { exact: true }),
  ).toBeVisible();
  await expect(
    page
      .getByRole("alert")
      .filter({ hasText: "Payment details could not be refreshed." }),
  ).toBeVisible();
  await expect(
    page
      .getByRole("alert")
      .filter({ hasText: "Order details could not be refreshed." }),
  ).toBeVisible();
  await expect(page.getByLabel("Refund amount")).toBeDisabled();
  await page
    .getByRole("button", { name: "Refresh payment details", exact: true })
    .click();
  await expectAmounts(page, "50.00", "175.00", "PartiallyRefunded");
  await expect(
    page.getByText("Request status: Completed.", { exact: true }),
  ).toBeVisible();
  await expect(page.getByLabel("Refund amount")).toBeEnabled();
  await expect(page.getByRole("alert")).toHaveCount(0);
});

async function resourceHarness(page: Page) {
  const errors: string[] = [];
  page.on("pageerror", (error) => errors.push(error.message));
  // A controlled loader deliberately ignores AbortSignal so the hook's stale
  // response guard is tested independently of Axios/network cancellation.
  // This route exists only in Playwright, never in the app or acceptance host.
  await page.route("**/__refund_resource_test", (route) =>
    route.fulfill({
      contentType: "text/html",
      body: `
    <div id="root"></div><script type="module">
    import React from '/Amaara-Creations/node_modules/.vite/deps/react.js';
    import ReactDOM from '/Amaara-Creations/node_modules/.vite/deps/react-dom_client.js';
    import { useResource } from '/Amaara-Creations/src/shared/hooks/useResource.js';
    const h = React.createElement;
    const requests = [];
    const old = { status: 'Paid', refundedAmount: 0, refundableAmount: 225, refunds: [] };
    const newer = { status: 'PartiallyRefunded', refundedAmount: 50, refundableAmount: 175, refunds: [{ status: 'Completed' }] };
    function Harness() {
      const [key, changeKey] = React.useState('payment:1');
      const resource = useResource(() => new Promise((resolve, reject) => requests.push({ resolve, reject })), key, true, { keepPreviousData: true });
      return h('main', {},
        h('p', { role: 'status' }, resource.loading ? 'Refreshing' : 'Ready'),
        resource.error && h('p', { role: 'alert' }, 'Refresh failed'),
        h('pre', { 'aria-label': 'Payment snapshot' }, JSON.stringify(resource.data)),
        h('button', { onClick: resource.reload }, 'Reload'),
        h('button', { onClick: () => changeKey('payment:2') }, 'Change payment'),
        h('button', { onClick: () => requests[0].resolve(old) }, 'Initial response'),
        h('button', { onClick: () => requests[1].resolve(old) }, 'Stale response'),
        h('button', { onClick: () => requests[2].resolve(newer) }, 'New response'),
        h('button', { onClick: () => requests[1].reject(new Error('Test refresh failure')) }, 'Failed response'));
    }
    ReactDOM.createRoot(document.getElementById('root')).render(h(Harness));
    </script>`,
    }),
  );
  await page.goto("./__refund_resource_test");
  await expect.poll(() => errors).toEqual([]);
  await page
    .getByRole("button", { name: "Initial response", exact: true })
    .click();
  await expect(page.getByRole("status")).toHaveText("Ready");
  await expect(page.getByLabel("Payment snapshot")).toContainText(
    '"refundedAmount":0',
  );
}

test("late pre-refund response cannot overwrite a newer refund snapshot even when the loader ignores cancellation", async ({
  page,
}) => {
  await resourceHarness(page);
  await page.getByRole("button", { name: "Reload", exact: true }).click();
  await expect(page.getByRole("status")).toHaveText("Refreshing");
  await expect(page.getByLabel("Payment snapshot")).toContainText(
    '"refundableAmount":225',
  );
  await page.getByRole("button", { name: "Reload", exact: true }).click();
  await page.getByRole("button", { name: "New response", exact: true }).click();
  await expect(page.getByRole("status")).toHaveText("Ready");
  await expect(page.getByLabel("Payment snapshot")).toContainText(
    '"refundedAmount":50',
  );
  await page
    .getByRole("button", { name: "Stale response", exact: true })
    .click();
  await expect(page.getByLabel("Payment snapshot")).toContainText(
    '"refundedAmount":50',
  );
  await expect(page.getByLabel("Payment snapshot")).toContainText(
    '"refundableAmount":175',
  );
  await expect(page.getByLabel("Payment snapshot")).toContainText(
    '"status":"Completed"',
  );
});

test("resource refresh error retains the last server snapshot and navigation clears a different payment key", async ({
  page,
}) => {
  await resourceHarness(page);
  await page.getByRole("button", { name: "Reload", exact: true }).click();
  await page
    .getByRole("button", { name: "Failed response", exact: true })
    .click();
  await expect(page.getByRole("alert")).toHaveText("Refresh failed");
  await expect(page.getByLabel("Payment snapshot")).toContainText(
    '"refundedAmount":0',
  );
  await page
    .getByRole("button", { name: "Change payment", exact: true })
    .click();
  await expect(page.getByLabel("Payment snapshot")).toHaveText("null");
  await expect(page.getByRole("status")).toHaveText("Refreshing");
  await page.getByRole("button", { name: "New response", exact: true }).click();
  await expect(page.getByLabel("Payment snapshot")).toContainText(
    '"refundedAmount":50',
  );
  await expect(page.getByRole("alert")).toHaveCount(0);
});
