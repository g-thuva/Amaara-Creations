export const sortOptions = [['newest', 'Newest'], ['price-asc', 'Price: low to high'], ['price-desc', 'Price: high to low'], ['name', 'Name'], ['featured', 'Featured first']];
export function catalogParams(params: URLSearchParams) {
  const result: Record<string, string | number | boolean> = { pageNumber: Math.min(1000000, Math.max(1, Math.trunc(Number(params.get('page')) || 1))), pageSize: 12, sort: 'newest' };
  for (const key of ['search', 'category', 'categoryId', 'collectionId', 'minPrice', 'maxPrice']) {
    const value = params.get(key)?.trim();
    if (value) result[key] = value;
  }
  if (sortOptions.some(([value]) => value === params.get('sort'))) result.sort = params.get('sort')!;
  if (params.get('inStock') === 'true') result.inStock = true;
  return result;
}
