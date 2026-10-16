export function safeLink(value: unknown): string | null {
  if (typeof value !== 'string' || /[\u0000-\u0020\\]/.test(value)) return null;
  if (value.startsWith('/') && !value.startsWith('//')) return value;
  try { const url = new URL(value); return ['https:', 'http:', 'mailto:', 'tel:'].includes(url.protocol) ? value : null; } catch { return null; }
}
