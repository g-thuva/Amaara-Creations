import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { AxiosError } from 'axios';

let api, session, refreshSession;
function reply(config, status, data = {}, headers = {}) {
  const response = { config, status, data, headers, statusText: String(status) };
  if (status >= 400) throw new AxiosError('fixture HTTP error', 'ERR_BAD_RESPONSE', config, null, response);
  return response;
}
beforeEach(async () => {
  vi.resetModules();
  ({ default: api, refreshSession } = await import('./httpClient.js'));
  session = await import('./authSession.js');
  session.beginAuthOperation(); session.setAccessToken('old');
});
afterEach(() => { vi.unstubAllGlobals(); vi.useRealTimers(); });

describe('session renewal', () => {
  it('coordinates concurrent expired requests, retries each once, and updates server user state', async () => {
    const accepted = vi.fn(); session.setSessionHandler(accepted);
    let renewals = 0; let mutations = 0;
    api.defaults.adapter = async config => {
      if (config.url.endsWith('/refresh')) { renewals++; await Promise.resolve(); return reply(config, 200, { token: 'new', user: { id: 'current', roles: ['Customer'] } }); }
      if (config.headers.Authorization === 'Bearer old') return reply(config, 401, {}, { 'x-auth-refresh': 'required' });
      if (config.method === 'post') mutations++;
      return reply(config, 200);
    };
    await Promise.all([api.get('/account'), api.get('/cart'), api.post('/wishlist', { productId: 1 })]);
    expect(renewals).toBe(1); expect(mutations).toBe(1); expect(session.getAccessToken()).toBe('new');
    expect(accepted).toHaveBeenCalledOnce();
  });

  it.each([403, 401, 500])('never refreshes or replays a business/network response with status %s', async status => {
    let attempts = 0;
    api.defaults.adapter = config => { attempts++; return Promise.resolve(reply(config, status)); };
    await expect(api.post('/orders', { value: 1 })).rejects.toBeTruthy();
    expect(attempts).toBe(1);
  });

  it.each([503, 429, 409, 403, 0])('preserves the current identity when renewal is temporarily unavailable (%s)', async status => {
    const failed = vi.fn(); session.setAuthFailureHandler(failed);
    api.defaults.adapter = async config => {
      if (config.url.endsWith('/refresh')) {
        if (!status) throw new AxiosError('Network unavailable', 'ERR_NETWORK', config);
        return reply(config, status);
      }
      return reply(config, 401, {}, { 'x-auth-refresh': 'required' });
    };
    await expect(api.get('/account')).rejects.toBeTruthy();
    expect(failed).not.toHaveBeenCalled(); expect(session.getAccessToken()).toBe('old');
  });

  it('expires the identity only after the server rejects the session', async () => {
    const failed = vi.fn(); session.setAuthFailureHandler(failed); let count = 0;
    api.defaults.adapter = async config => { count++; return reply(config, 401, {}, { 'x-auth-refresh': 'required' }); };
    await expect(api.get('/account')).rejects.toBeTruthy();
    expect(count).toBe(2); expect(failed).toHaveBeenCalledOnce(); expect(session.getAccessToken()).toBeNull();
  });

  it('uses an already renewed token for a late 401 instead of rotating the cookie again', async () => {
    let calls = 0;
    api.defaults.adapter = async config => {
      calls++;
      if (config.headers.Authorization === 'Bearer old') { session.setAccessToken('new'); return reply(config, 401, {}, { 'x-auth-refresh': 'required' }); }
      return reply(config, 200);
    };
    await api.get('/late-response'); expect(calls).toBe(2);
  });

  it('does not restore an in-flight refresh after logout or a newer sign-in', async () => {
    let finish;
    api.defaults.adapter = config => new Promise(resolve => { finish = () => resolve(reply(config, 200, { token: 'obsolete', user: {} })); });
    const refresh = refreshSession(); await vi.waitFor(() => expect(finish).toBeTypeOf('function'));
    session.beginAuthOperation(); session.clearAccessToken(); finish();
    await expect(refresh).rejects.toMatchObject({ code: 'ERR_CANCELED' }); expect(session.getAccessToken()).toBeNull();
  });

  it('retries a rotation conflict at most once without accepting the old credential', async () => {
    vi.useFakeTimers(); let count = 0;
    api.defaults.adapter = async config => { count++; return reply(config, 409, { code: 'SESSION_REFRESH_CONFLICT' }); };
    const outcome = refreshSession().catch(error => error);
    await vi.runAllTimersAsync(); expect((await outcome).response.status).toBe(409); expect(count).toBe(2);
  });

  it('takes a cross-tab Web Lock without sharing credentials', async () => {
    const request = vi.fn(async (_key, _options, callback) => callback()); vi.stubGlobal('navigator', { locks: { request } });
    api.defaults.adapter = async config => reply(config, 200, { token: 'new', user: {} });
    await refreshSession(); expect(request).toHaveBeenCalledOnce();
    expect(request.mock.calls[0][0]).not.toContain('old');
  });
});
