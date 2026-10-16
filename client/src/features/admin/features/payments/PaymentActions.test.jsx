import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { PaymentActions } from "@/features/admin/features/payments/PaymentActions.jsx";
import { RefundForm } from "@/features/admin/features/payments/RefundForm.jsx";

describe("financial action safeguards", () => {
  it("blocks all provider operations when authoritative state is uncertain", () => {
    const html = renderToStaticMarkup(
      <PaymentActions
        payment={{ status: "Authorized" }}
        busy
        pending={false}
        loading={false}
      />,
    );
    expect(html).toContain("Settle authorized payment");
    expect(html).toContain("Reverse authorization");
    expect(html.match(/disabled=""/g)).toHaveLength(3);
  });
  it("does not expose authorization operations for a paid attempt", () => {
    expect(
      renderToStaticMarkup(<PaymentActions payment={{ status: "Paid" }} />),
    ).not.toContain("Reverse authorization");
  });
  it("preserves refund bounds and disables inputs during unsafe state", () => {
    const html = renderToStaticMarkup(
      <RefundForm payment={{ refundableAmount: 25, currency: "LKR" }} busy />,
    );
    expect(html).toContain('max="25"');
    expect(html).toContain('min="0.01"');
    expect(html).toContain('maxLength="500"');
    expect(html.match(/disabled=""/g)).toHaveLength(3);
  });
});
