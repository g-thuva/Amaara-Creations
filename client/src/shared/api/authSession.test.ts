import { describe, expect, it, vi } from 'vitest';
import { clearAccessToken, getAccessToken, notifyAuthFailure, setAccessToken, setAuthFailureHandler } from '@/shared/api/authSession.js';
import { beginAuthOperation, isCurrentAuthOperation } from '@/shared/api/authSession.js';

describe('authSession', () => {
  it('invalidates an older restore or request when explicit sign-in or sign-out starts', () => {
    const restoring = beginAuthOperation();
    const signingIn = beginAuthOperation();
    expect(isCurrentAuthOperation(restoring)).toBe(false);
    expect(isCurrentAuthOperation(signingIn)).toBe(true);
    const signingOut = beginAuthOperation();
    expect(isCurrentAuthOperation(signingIn)).toBe(false);
    expect(isCurrentAuthOperation(signingOut)).toBe(true);
  });
  it('stores access tokens only in module memory', () => {
    setAccessToken('access-token');
    expect(getAccessToken()).toBe('access-token');

    clearAccessToken();
    expect(getAccessToken()).toBeNull();
  });

  it('clears token and notifies on auth failure', () => {
    const handler = vi.fn();
    setAccessToken('access-token');
    setAuthFailureHandler(handler);

    notifyAuthFailure();

    expect(getAccessToken()).toBeNull();
    expect(handler).toHaveBeenCalledOnce();
  });
});
