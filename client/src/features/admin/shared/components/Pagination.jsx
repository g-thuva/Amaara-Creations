import { pagination } from "@/features/admin/shared/utils/pagination.js";

export function Pagination({
  page,
  setPage,
  total,
  size,
  data,
  label = "Results pages",
  disabled = false,
}) {
  const info = pagination({ page, pageSize: size, totalCount: total, ...data });
  return (
    <nav className="admin-pagination" aria-label={label}>
      <button
        className="btn btn-outline-primary"
        disabled={disabled || !info.hasPrevious}
        onClick={() => setPage(info.page - 1)}
      >
        Previous
      </button>
      <span aria-current="page" aria-live="polite">
        Page {info.page} of {info.pages} · {info.total} records
      </span>
      <button
        className="btn btn-outline-primary"
        disabled={disabled || !info.hasNext}
        onClick={() => setPage(info.page + 1)}
      >
        Next
      </button>
    </nav>
  );
}
