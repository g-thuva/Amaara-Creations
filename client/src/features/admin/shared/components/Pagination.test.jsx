import { describe, expect, it } from "vitest";
import { renderToStaticMarkup } from "react-dom/server";
import { Pagination } from "@/features/admin/shared/components/Pagination.jsx";
import { pagination } from "@/features/admin/shared/utils/pagination.js";

describe("server pagination", () => {
  it("normalizes the legacy order/customer pageNumber metadata", () => {
    expect(
      pagination({ page: 1, pageNumber: 2, pageSize: 20, totalCount: 41 }),
    ).toMatchObject({ page: 2, pages: 3, hasPrevious: true, hasNext: true });
  });
  it("uses server page size and total count for balances", () => {
    expect(pagination({ page: 2, pageSize: 10, totalCount: 21 })).toMatchObject(
      { pages: 3, hasNext: true },
    );
    const html = renderToStaticMarkup(
      <Pagination
        page={1}
        data={{ page: 2, pageSize: 10, totalCount: 21 }}
        setPage={() => {}}
        label="Stock balance pages"
      />,
    );
    expect(html).toContain("Page 2 of 3");
    expect(html).toContain('aria-label="Stock balance pages"');
  });
  it("handles empty results and the final movement page", () => {
    expect(pagination({ totalCount: 0 })).toMatchObject({
      pages: 1,
      hasNext: false,
      hasPrevious: false,
    });
    expect(pagination({ page: 2, pageSize: 50, totalCount: 51 })).toMatchObject(
      { pages: 2, hasNext: false, hasPrevious: true },
    );
  });
});
