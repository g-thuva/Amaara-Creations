import { safeReturnDestination } from './returnDestination.js';

const staffRoles = new Set(['Admin', 'SuperAdmin', 'CatalogManager', 'ProductionStaff', 'CustomerSupport', 'Marketing', 'Analyst']);
const loopbackHosts = new Set(['localhost', '127.0.0.1', '[::1]']);

// Navigation uses only the identity returned by a successful sign-in. It grants
// no management access: that application verifies its own session and MFA.
export function loginDestination(user, from, {
  managementLoginUrl = import.meta.env.VITE_MANAGEMENT_LOGIN_URL,
  development = import.meta.env.DEV,
  pageUrl = globalThis.location?.href,
  basePath = import.meta.env.BASE_URL,
  managementReturn = globalThis.location ? new URLSearchParams(globalThis.location.search).get('returnTo') : null,
} = {}) {
  const roles = Array.isArray(user?.roles) ? user.roles : [user?.role];
  if (!roles.some(role => staffRoles.has(role))) {
    return { external: false, url: safeReturnDestination(from) };
  }

  try {
    const current = new URL(pageUrl);
    let configured = managementLoginUrl?.trim();
    if (!configured && development && loopbackHosts.has(current.hostname)) {
      const local = new URL(basePath, current.origin);
      local.port = '5179';
      local.pathname = local.pathname.replace(/\/?$/, '/') + 'login';
      configured = local.href;
    }
    if (!configured || /[\\\s]/.test(configured)) return null;
    const target = new URL(configured);
    const localHttp = development && target.protocol === 'http:' && loopbackHosts.has(target.hostname);
    if ((target.protocol !== 'https:' && !localHttp) || target.username || target.password || target.search || target.hash || target.origin === current.origin) return null;
    if (!development && loopbackHosts.has(target.hostname)) return null;
    const safeReturn = safeReturnDestination(managementReturn, '', true);
    if (safeReturn) target.searchParams.set('returnTo', safeReturn);
    return { external: true, url: target.href };
  } catch {
    return null;
  }
}
