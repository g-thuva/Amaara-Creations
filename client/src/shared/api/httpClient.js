import axios from 'axios';
import { API_BASE_URL, AUTH_BASE_PATH } from '@/shared/api/requestConfig.js';
import { currentAuthOperation, isCurrentAuthOperation, getAccessToken, notifyAuthFailure, acceptSession } from '@/shared/api/authSession.js';

const api = axios.create({ baseURL: API_BASE_URL, withCredentials: true, headers: { 'Content-Type': 'application/json' } });
let refreshPromise = null;
let refreshController = null;
export const isSessionRejected = error => error.response?.status === 401;
export const cancelSessionRefresh = () => {
  const controller = refreshController;
  refreshPromise = null; refreshController = null;
  controller?.abort();
};
export const refreshSession = () => {
  if (refreshPromise) return refreshPromise;
  const ticket = currentAuthOperation();
  const controller = new AbortController();
  refreshController = controller;
  const renew = async () => {
    if (!isCurrentAuthOperation(ticket) || controller.signal.aborted) throw new axios.CanceledError();
    const send = () => api.post(`${AUTH_BASE_PATH}/refresh`, undefined, { signal: controller.signal, _skipAuthRefresh: true });
    let response;
    try { response = await send(); }
    catch (error) {
      // No token is returned for a superseded credential. Let a sibling's
      // Set-Cookie finish, then try once using the shared browser cookie jar.
      if (error.response?.status !== 409 || error.response?.data?.code !== 'SESSION_REFRESH_CONFLICT') throw error;
      await new Promise(resolve => setTimeout(resolve, 300));
      response = await send();
    }
    if (!isCurrentAuthOperation(ticket)) throw new axios.CanceledError();
    acceptSession(response.data);
    return response;
  };
  // Web Locks serialize cookie exchanges across tabs of the same application.
  // The backend's atomic conflict handling also covers unsupported browsers.
  const locks = globalThis.navigator?.locks;
  const request = (locks ? locks.request(`amaara.refresh:${API_BASE_URL}:${AUTH_BASE_PATH}`, { signal: controller.signal }, renew) : renew())
    .finally(() => { if (refreshPromise === request) { refreshPromise = null; refreshController = null; } });
  refreshPromise = request;
  return request;
};
api.interceptors.request.use(config => {
  config._authGeneration ??= currentAuthOperation();
  if (config._authGeneration !== currentAuthOperation()) throw new axios.CanceledError();
  const token = getAccessToken();
  if (token && !config._skipAuthRefresh) config.headers.Authorization = `Bearer ${token}`;
  return config;
});
api.interceptors.response.use(response => response, async error => {
  const request = error.config;
  // The marker is emitted only by bearer authentication before endpoint execution.
  // Business 401s, every 403, and ambiguous network failures are never replayed.
  if (error.response?.status !== 401 || error.response.headers?.['x-auth-refresh'] !== 'required' || !request || request._retry || request._skipAuthRefresh || request._authGeneration !== currentAuthOperation()) throw error;
  request._retry = true;
  const ticket = currentAuthOperation();
  try {
    const latest = getAccessToken();
    if (!latest || request.headers.Authorization === `Bearer ${latest}`) await refreshSession();
    if (!isCurrentAuthOperation(ticket)) throw new axios.CanceledError();
    request.headers.Authorization = `Bearer ${getAccessToken()}`;
    return await api(request);
  } catch (failure) {
    if (isCurrentAuthOperation(ticket) && isSessionRejected(failure)) notifyAuthFailure();
    throw failure;
  }
});
export default api;
