import axios from 'axios';
import { API_BASE_URL, AUTH_BASE_PATH } from '@/shared/api/requestConfig.js';
import { currentAuthOperation, isCurrentAuthOperation, getAccessToken, notifyAuthFailure, setAccessToken } from '@/shared/api/authSession.js';

// Create axios instance
const api = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

let refreshPromise = null;
let refreshController = null;
export const cancelSessionRefresh = () => {
  const controller = refreshController;
  refreshPromise = null; refreshController = null;
  controller?.abort();
};
export const refreshSession = () => {
  if (!refreshPromise) {
    refreshController = new AbortController();
    const request = api.post(`${AUTH_BASE_PATH}/refresh`, undefined, { signal: refreshController.signal }).finally(() => {
      if (refreshPromise === request) { refreshPromise = null; refreshController = null; }
    });
    refreshPromise = request;
  }
  return refreshPromise;
};

const isAuthRefreshRequest = (config) => {
  const url = (config?.url || '').toLowerCase();
  return /\/auth\/(?:staff\/)?(?:refresh|login|sign-in|register)(?:[/?]|$)/.test(url);
};

// Request interceptor - Add auth token to requests
api.interceptors.request.use(
  (config) => {
    config._authGeneration ??= currentAuthOperation();
    const token = getAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }

    return config;
  },
  (error) => {
    return Promise.reject(error);
  }
);

// Response interceptor - Handle errors globally
api.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config;

    if (error.response?.status === 401 && originalRequest && originalRequest._authGeneration === currentAuthOperation() && !originalRequest._retry && !isAuthRefreshRequest(originalRequest)) {
      originalRequest._retry = true;
      const ticket = currentAuthOperation();

      try {
        const response = await refreshSession();
        if (!isCurrentAuthOperation(ticket)) return Promise.reject(error);
        setAccessToken(response.data.token);
        originalRequest.headers.Authorization = `Bearer ${response.data.token}`;
        return api(originalRequest);
      } catch (refreshError) {
        if (isCurrentAuthOperation(ticket)) notifyAuthFailure();
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default api;

