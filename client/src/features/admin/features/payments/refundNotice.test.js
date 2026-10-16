import { describe, expect, it } from "vitest";
import { refundNotice } from "@/features/admin/features/payments/refundNotice.js";

describe("server-authoritative refund notifications", () => {
  it("announces completion only when the refund DTO confirms it", () => {
    expect(refundNotice({ status: "Completed" })).toEqual({
      success: true,
      text: "Request status: Completed.",
    });
  });
  it("keeps a provider-pending credit distinct from completion", () => {
    expect(refundNotice({ status: "Pending" })).toMatchObject({
      text: "Request status: Pending.",
    });
    expect(refundNotice({ status: "Pending" }).success).not.toBe(true);
    expect(refundNotice({ status: "Pending" }).detail).toContain(
      "Awaiting provider verification",
    );
  });
  it("shows an actionable failure even when the HTTP request succeeded", () => {
    expect(refundNotice({ status: "Failed" })).toMatchObject({
      error: true,
      text: "Request status: Failed.",
    });
    expect(refundNotice({ status: "Failed" }).success).not.toBe(true);
    expect(refundNotice({ status: "Failed" }).detail).toContain(
      "before trying again",
    );
  });
  it.each([
    undefined,
    {},
    { status: 204 },
    { status: "Paid" },
    { status: "Unknown" },
  ])(
    "never invents completion from an empty or unrelated response: %j",
    (result) => {
      expect(refundNotice(result)).toMatchObject({
        error: true,
        text: "Refund status could not be verified.",
      });
      expect(refundNotice(result).success).not.toBe(true);
    },
  );
});
