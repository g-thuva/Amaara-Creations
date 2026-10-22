export function safeReturnDestination(value, fallback = '/profile', management = false) {
  if (typeof value !== 'string' || !value.startsWith('/') || value.startsWith('//') || /\\|%2f|%5c/i.test(value) || Array.from(value).some(character => character.charCodeAt(0) <= 32)) return fallback;
  const url = new URL(value, 'https://local.invalid');
  if (url.origin !== 'https://local.invalid' || /^\/(login|register|reset-password|forgot-password)(\/|$)/.test(url.pathname)) return fallback;
  const staff = /^\/admin(?:\/|$)/.test(url.pathname);
  return staff === management ? url.pathname + url.search + url.hash : fallback;
}
