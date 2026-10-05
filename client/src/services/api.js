import axios from 'axios';
import { API_BASE_URL } from './config';
import { getAccessToken, notifyAuthFailure, setAccessToken } from './authSession';

// Create axios instance
const api = axios.create({
  baseURL: API_BASE_URL,
  withCredentials: true,
  headers: {
    'Content-Type': 'application/json',
  },
});

let refreshPromise = null;
export const refreshSession = () => {
  refreshPromise ??= api.post('/auth/refresh').finally(() => { refreshPromise = null; });
  return refreshPromise;
};

const isAuthRefreshRequest = (config) => {
  const url = (config?.url || '').toLowerCase();
  return url.includes('/auth/refresh') || url.includes('/auth/login') || url.includes('/auth/register');
};

// Request interceptor - Add auth token to requests
api.interceptors.request.use(
  (config) => {
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

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry && !isAuthRefreshRequest(originalRequest)) {
      originalRequest._retry = true;

      try {
        const response = await refreshSession();
        setAccessToken(response.data.token);
        originalRequest.headers.Authorization = `Bearer ${response.data.token}`;
        return api(originalRequest);
      } catch (refreshError) {
        notifyAuthFailure();
        return Promise.reject(refreshError);
      }
    }

    return Promise.reject(error);
  }
);

export default api;

