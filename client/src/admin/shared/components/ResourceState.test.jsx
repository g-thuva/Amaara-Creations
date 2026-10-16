import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { ResourceState } from "./ResourceState.jsx";

const render = (resource) =>
  renderToStaticMarkup(
    <ResourceState resource={{ reload() {}, ...resource }}>
      <p>Trusted snapshot</p>
    </ResourceState>,
  );
describe("resource presentation", () => {
  it("announces initial loading without displaying empty content", () => {
    const html = render({ loading: true, data: null });
    expect(html).toContain('role="status"');
    expect(html).not.toContain("Trusted snapshot");
  });
  it("retains data during refresh and refresh failures", () => {
    expect(render({ loading: true, data: {} })).toContain("Trusted snapshot");
    const html = render({ data: {}, error: { response: { status: 503 } } });
    expect(html).toContain('role="alert"');
    expect(html).toContain("Trusted snapshot");
  });
  it("exposes permission failure and retry without content", () => {
    const html = render({ data: null, error: { response: { status: 403 } } });
    expect(html).toContain("You do not have permission");
    expect(html).toContain("Try again");
    expect(html).not.toContain("Trusted snapshot");
  });
});
