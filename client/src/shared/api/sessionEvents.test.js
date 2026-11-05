import { afterEach, expect, it, vi } from 'vitest';
import { publishSessionEvent, subscribeSessionEvents } from './sessionEvents.js';

afterEach(() => vi.unstubAllGlobals());
it('shares lifecycle signals without access credentials, identity or passwords', () => {
  const setItem = vi.fn(); vi.stubGlobal('localStorage', { setItem });
  publishSessionEvent('signed-out');
  expect(setItem).toHaveBeenCalledOnce();
  const payload = JSON.parse(setItem.mock.calls[0][1]);
  expect(Object.keys(payload).sort()).toEqual(['nonce', 'type']);
  expect(payload.type).toBe('signed-out');
});
it('handles matching lifecycle signals, ignores unrelated storage, and releases its listener', () => {
  let receive; const removeEventListener = vi.fn();
  vi.stubGlobal('window', { addEventListener: (_type, listener) => { receive = listener; }, removeEventListener });
  let key; vi.stubGlobal('localStorage', { setItem: name => { key = name; } });
  publishSessionEvent('signed-in');
  const listener = vi.fn(); const stop = subscribeSessionEvents(listener);
  receive({ key: 'unrelated', newValue: '{"type":"signed-out"}' });
  receive({ key, newValue: 'malformed' });
  receive({ key, newValue: '{"type":"unexpected"}' });
  receive({ key, newValue: '{"type":"signed-out"}' });
  expect(listener).toHaveBeenCalledExactlyOnceWith('signed-out');
  stop(); expect(removeEventListener).toHaveBeenCalledWith('storage', receive);
});
