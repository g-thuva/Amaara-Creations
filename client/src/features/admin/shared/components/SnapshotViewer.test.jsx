import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { SnapshotViewer } from "@/features/admin/shared/components/SnapshotViewer.jsx";

describe("historical snapshots", () => {
  it("formats valid JSON", () => {
    const html = renderToStaticMarkup(
      <SnapshotViewer value={'{"quantity":12}'} />,
    );
    expect(html).toContain("&quot;quantity&quot;: 12");
  });

  it("keeps surrounding content visible and escapes malformed historical text", () => {
    const html = renderToStaticMarkup(
      <>
        <SnapshotViewer value={"{bad<script>alert(1)</script>"} />
        <h2>Other order details</h2>
      </>,
    );
    expect(html).toContain("could not be read");
    expect(html).toContain("Other order details");
    expect(html).toContain("&lt;script&gt;");
    expect(html).not.toContain("<script>");
  });

  it("handles missing snapshots", () => {
    expect(renderToStaticMarkup(<SnapshotViewer value={null} />)).toContain(
      "No snapshot is available.",
    );
  });
});
