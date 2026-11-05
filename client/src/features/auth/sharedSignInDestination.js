import { safeReturnDestination } from './returnDestination.js';

export function sharedSignInDestination(currentHref, site = import.meta.env.VITE_PUBLIC_SITE_URL, base = import.meta.env.BASE_URL) {
  try {
    const current = new URL(currentHref);
    const target = site ? new URL(site) : new URL(base, current.origin);
    const local = ['localhost', '127.0.0.1', '[::1]'];
    if (!site) {
      if (!import.meta.env.DEV || !local.includes(current.hostname)) return null;
      target.port = '5179';
    }
    if (!['http:', 'https:'].includes(target.protocol) || target.username || target.password || target.search || target.hash || target.origin === current.origin) return null;
    if (target.protocol === 'http:' && (!import.meta.env.DEV || !local.includes(target.hostname))) return null;
    const prefix = base.replace(/\/$/, '');
    const path = current.pathname.startsWith(prefix + '/') ? current.pathname.slice(prefix.length) : current.pathname;
    const accountPage = /^\/(register|forgot-password|reset-password|verify-email)\/?$/.test(path);
    target.pathname = target.pathname.replace(/\/?$/, '/') + (accountPage ? path.slice(1) : 'login');
    if (accountPage) target.search = current.search;
    else {
      const requested = safeReturnDestination(path + current.search, '', true)
        || safeReturnDestination(current.searchParams.get('returnTo'), '', true);
      if (requested) target.searchParams.set('returnTo', requested);
    }
    return target.href;
  } catch { return null; }
}
