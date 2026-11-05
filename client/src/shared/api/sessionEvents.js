import { AUTH_BASE_PATH, API_BASE_URL } from './requestConfig.js';

// Messages contain only lifecycle signals, never credentials or user data.
const key = `amaara.session:${API_BASE_URL}:${AUTH_BASE_PATH}`;
export function publishSessionEvent(type) {
  try { localStorage.setItem(key, JSON.stringify({ type, nonce: crypto.randomUUID() })); } catch { /* Storage may be unavailable. */ }
}
export function subscribeSessionEvents(listener) {
  const receive = event => {
    if (event.key !== key || !event.newValue) return;
    try { const { type } = JSON.parse(event.newValue); if (['signed-in', 'signed-out'].includes(type)) listener(type); } catch { /* Ignore unrelated malformed storage. */ }
  };
  window.addEventListener('storage', receive);
  return () => window.removeEventListener('storage', receive);
}
