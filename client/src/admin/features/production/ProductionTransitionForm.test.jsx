import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { ProductionTransitionForm } from "./ProductionTransitionForm";

describe("production transition safeguards", () => {
  it("offers only server-approved transitions and requires a note", () => {
    const html = renderToStaticMarkup(
      <ProductionTransitionForm
        data={{ availableProductionTransitions: ["Printing"] }}
        pending
      />,
    );
    expect(html).toContain("<option>Printing</option>");
    expect(html).not.toContain("<option>Ready</option>");
    expect(html).toMatch(/name="note"[^>]*required/);
    expect(html).toContain('disabled=""');
  });
  it("explains why a job with no transitions cannot advance", () => {
    const html = renderToStaticMarkup(
      <ProductionTransitionForm
        data={{ availableProductionTransitions: [] }}
      />,
    );
    expect(html).toContain("No transition available.");
    expect(html).not.toContain("<form");
  });
});
