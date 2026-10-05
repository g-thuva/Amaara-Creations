export const money = (value: number, currency = 'LKR') => new Intl.NumberFormat('en-LK', { style: 'currency', currency, currencyDisplay: 'code', minimumFractionDigits: 2 }).format(value);
export const date = (value: string) => new Intl.DateTimeFormat('en-LK', { dateStyle: 'medium', timeZone: 'Asia/Colombo' }).format(new Date(value));
export const validQuantity = (value: number, max = Number.MAX_SAFE_INTEGER) => Number.isSafeInteger(value) && value >= 1 && value <= max;
export function safeLink(value: unknown): string | null {
  if (typeof value !== 'string' || /[\u0000-\u0020\\]/.test(value)) return null;
  if (value.startsWith('/') && !value.startsWith('//')) return value;
  try { const url = new URL(value); return ['https:', 'http:', 'mailto:', 'tel:'].includes(url.protocol) ? value : null; } catch { return null; }
}
export function customerError(error: { response?: { status?: number, data?: { message?: string, errors?: Record<string, string[]> | string[] } } }, fallback = 'We could not complete your request. Please try again.') {
  const status = error?.response?.status;
  if (!status) return 'Unable to connect. Check your connection and try again.';
  if (status === 401) return 'Your session has ended. Please sign in again.';
  if (status === 403) return 'You do not have access to this action.';
  if (status === 404) return 'This item is no longer available.';
  if (status === 409) return 'This item changed. Refresh and try again.';
  if (status === 429) return 'Too many attempts. Please wait a moment and try again.';
  if (status === 400) {
    const errors = error.response?.data?.errors;
    return errors ? Object.values(errors).flat().join(' ') : error.response?.data?.message || fallback;
  }
  return fallback;
}
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
