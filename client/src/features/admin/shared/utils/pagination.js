export function pagination({
  page = 1,
  pageNumber,
  pageSize = 20,
  totalCount = 0,
  totalItems,
  totalPages,
}) {
  const total = totalItems ?? totalCount;
  const pages = Math.max(1, totalPages ?? Math.ceil(total / pageSize));
  const currentPage = pageNumber ?? page;
  return {
    page: currentPage,
    total,
    pages,
    hasPrevious: currentPage > 1,
    hasNext: currentPage < pages,
  };
}
