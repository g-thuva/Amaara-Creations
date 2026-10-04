import { describe, expect, it, vi } from 'vitest';
import { clearAccessToken, getAccessToken, notifyAuthFailure, setAccessToken, setAuthFailureHandler } from './authSession';

describe('authSession', () => {
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
