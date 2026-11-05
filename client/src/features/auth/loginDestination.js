import { safeReturnDestination } from './returnDestination.js';
import { acceptsIdentity } from './sessionIdentity.js';

const loopbackHosts = new Set(['localhost', '127.0.0.1', '[::1]']);

// The completed server response chooses navigation. The management app restores
// its own HttpOnly session and independently checks permissions.
export function loginDestination(user, from, {
  managementLoginUrl = import.meta.env.VITE_MANAGEMENT_LOGIN_URL,
  development = import.meta.env.DEV,
  pageUrl = globalThis.location?.href,
  basePath = import.meta.env.BASE_URL,
  managementReturn = globalThis.location ? new URLSearchParams(globalThis.location.search).get('returnTo') : null,
} = {}) {
  if (acceptsIdentity(user)) return { external: false, url: safeReturnDestination(from) };
  if (!acceptsIdentity(user, true)) return null;
  try {
    const current = new URL(pageUrl);
    let configured = managementLoginUrl?.trim();
    if (!configured && development && loopbackHosts.has(current.hostname)) {
      const local = new URL(basePath, current.origin);
      local.port = '5173';
      local.pathname = local.pathname.replace(/\/?$/, '/') + 'login';
      configured = local.href;
    }
    if (!configured || /[\\\s]/.test(configured)) return null;
    const target = new URL(configured);
    const localHttp = development && target.protocol === 'http:' && loopbackHosts.has(target.hostname);
    if ((target.protocol !== 'https:' && !localHttp) || target.username || target.password || target.search || target.hash || target.origin === current.origin) return null;
    if (!development && loopbackHosts.has(target.hostname)) return null;
    const requested = safeReturnDestination(managementReturn || from, '', true);
    if (requested) target.searchParams.set('returnTo', requested);
    return { external: true, url: target.href };
  } catch {
    return null;
  }
}
