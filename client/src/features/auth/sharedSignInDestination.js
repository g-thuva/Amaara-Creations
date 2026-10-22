import { safeReturnDestination } from './returnDestination.js';

export function sharedSignInDestination(currentHref, site = import.meta.env.VITE_PUBLIC_SITE_URL, base = import.meta.env.BASE_URL) {
  try {
    const current = new URL(currentHref);
    const target = site ? new URL(site) : new URL(base, current.origin);
    if (!site) {
      if (!['localhost', '127.0.0.1', '[::1]'].includes(current.hostname)) return null;
      target.port = '5173';
    }
    if (!['http:', 'https:'].includes(target.protocol) || target.username || target.password || target.search || target.hash || target.origin === current.origin) return null;
    if (target.protocol === 'http:' && !['localhost', '127.0.0.1', '[::1]'].includes(target.hostname)) return null;
    target.pathname = target.pathname.replace(/\/?$/, '/') + 'login';
    const requested = safeReturnDestination(current.pathname.replace(base.replace(/\/$/, ''), ''), '', true);
    if (requested) target.searchParams.set('returnTo', requested + current.search);
    return target.href;
  } catch { return null; }
}
