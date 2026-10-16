import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { InventoryAdjustmentForm } from "./InventoryAdjustmentForm";
import { InventoryMovementsTable } from "./InventoryMovementsTable";

describe("inventory controls and ledger", () => {
  it("requires a reason and preserves the selected available balance", () => {
    const html = renderToStaticMarkup(
      <InventoryAdjustmentForm
        selected={{
          productId: 1,
          variantId: null,
          product: "Label",
          variant: "Base stock",
          available: 4,
          lowStockThreshold: 2,
        }}
        pending
      />,
    );
    expect(html).toContain("Available when selected: 4");
    expect(html).toMatch(/name="reason"[^>]*required/);
    expect(html).toContain('maxLength="300"');
    expect(html).toContain('disabled=""');
  });
  it("renders the server movement window with accessible headings", () => {
    const html = renderToStaticMarkup(
      <InventoryMovementsTable
        movements={{
          data: {
            items: [
              {
                id: 51,
                createdAt: "2026-01-01T00:00:00Z",
                quantityDelta: -3,
                newQuantity: 7,
                reason: "Recorded count",
                movementType: "Adjustment",
              },
            ],
          },
        }}
      />,
    );
    expect(html).toContain("Available-stock movement history");
    expect(html).toContain('scope="col"');
    expect(html).toContain("Recorded count");
    expect(html).toContain("-3");
  });
});
