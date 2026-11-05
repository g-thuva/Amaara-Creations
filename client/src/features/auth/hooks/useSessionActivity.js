import { useEffect } from 'react';
import { authApi } from '../authApi.js';

// Only trusted input in a visible document records activity. Timers, focus,
// polling and silent renewal never slide the server's idle deadline.
export function useSessionActivity(authenticated) {
  useEffect(() => {
    if (!authenticated) return undefined;
    let lastSent = 0;
    const activity = event => {
      if (!event.isTrusted || document.visibilityState !== 'visible' || Date.now() - lastSent < 60000) return;
      lastSent = Date.now();
      authApi.activity().catch(() => { /* HTTP client handles actual expiration; temporary errors preserve identity. */ });
    };
    window.addEventListener('pointerdown', activity, { passive: true });
    window.addEventListener('keydown', activity);
    return () => { window.removeEventListener('pointerdown', activity); window.removeEventListener('keydown', activity); };
  }, [authenticated]);
}
