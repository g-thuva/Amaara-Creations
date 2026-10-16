import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { OrderTotals } from "@/features/admin/features/orders/OrderTotals.jsx";
import { OrderTimeline } from "@/features/admin/features/orders/OrderTimeline.jsx";

describe("order history presentation", () => {
  it("uses the historical currency and recorded totals", () => {
    const html = renderToStaticMarkup(
      <OrderTotals
        order={{
          currency: "CHF",
          shippingTotal: 10,
          subtotal: 100,
          discountTotal: 5,
          taxTotal: 2,
          paymentFee: 3,
          shippingMethodName: "Historical courier",
          couponCode: "SAVED",
        }}
      />,
    );
    expect(html).toMatch(/CHF\s100\.00/);
    expect(html).toMatch(/CHF\s5\.00/);
    expect(html).toContain("SAVED");
  });
  it("renders recorded transitions and safely escapes reasons", () => {
    const html = renderToStaticMarkup(
      <OrderTimeline
        order={{
          history: [
            {
              createdAt: "2026-01-01T00:00:00Z",
              previousStatus: "Pending",
              newStatus: "Confirmed",
              reason: "<script>",
            },
          ],
        }}
      />,
    );
    expect(html).toContain("Pending");
    expect(html).toContain("Confirmed");
    expect(html).toContain("&lt;script&gt;");
  });
});
