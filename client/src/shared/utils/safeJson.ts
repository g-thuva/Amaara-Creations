// Historical/CMS/browser storage data is untrusted and may be malformed.
export function safeJson<T = unknown>(value: unknown, fallback: T | null = null): T | null {
  if (typeof value !== 'string') return fallback;
  try { return JSON.parse(value) as T; } catch { return fallback; }
}
