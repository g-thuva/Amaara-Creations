const loopbackHosts = new Set(['localhost', '127.0.0.1', '[::1]']);

// Check a local development destination before leaving the completed sign-in.
// This page probe carries no authentication credentials.
export async function verifyStaffWorkspace(url, {
  development = import.meta.env.DEV,
  fetchPage = globalThis.fetch,
} = {}) {
  const target = new URL(url);
  if (!development || !loopbackHosts.has(target.hostname)) return;
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), 2500);
  try {
    await fetchPage(target.href, { mode: 'no-cors', credentials: 'omit', cache: 'no-store', signal: controller.signal });
  } finally {
    clearTimeout(timeout);
  }
}
