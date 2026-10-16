export function publicBase(value) {
  if (!value) return null;
  try { const url = new URL(value); return url.protocol === 'https:' && !url.username && !url.password && !url.search && !url.hash ? url.href.replace(/\/+$/, '') : null; } catch { return null; }
}
