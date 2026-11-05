import api, { cancelSessionRefresh, refreshSession, isSessionRejected } from '@/shared/api/httpClient.js';
import { AUTH_BASE_PATH } from '@/shared/api/requestConfig.js';

export const authApi = {
  cancelRefresh: cancelSessionRefresh,
  isSessionRejected,
  activity: () => api.post('/auth/activity'),
  register: async (userData) => {
    const response = await api.post('/auth/register', userData);
    return response.data;
  },

  login: async (credentials) => {
    const response = await api.post(`${AUTH_BASE_PATH}/sign-in`, credentials);
    return response.data;
  },
  pendingSignIn: async () => (await api.get(`${AUTH_BASE_PATH}/sign-in`)).data,
  advanceSignIn: async (action, values = {}) => {
    const paths = { verify: 'verify', confirm: 'enrollment/confirm', complete: 'complete', cancel: 'cancel' };
    if (!paths[action]) throw new Error('Unknown sign-in step');
    return (await api.post(`${AUTH_BASE_PATH}/sign-in/${paths[action]}`, values)).data;
  },

  logout: async () => {
    await api.post(`${AUTH_BASE_PATH}/logout`, undefined, { _skipAuthRefresh: true });
  },

  logoutAll: async () => {
    const response = await api.post('/auth/logout-all');
    return response.data;
  },

  refresh: async () => {
    const response = await refreshSession();
    return response.data;
  },


  changePassword: async (passwordData) => {
    const response = await api.post('/auth/change-password', passwordData);
    return response.data;
  },

  forgotPassword: async (email) => {
    const response = await api.post('/auth/forgot-password', { email });
    return response.data;
  },

  resetPassword: async (resetData) => {
    const response = await api.post('/auth/reset-password', resetData);
    return response.data;
  },

  confirmEmail: async (data) => {
    const response = await api.post('/auth/confirm-email', data);
    return response.data;
  },

  resendConfirmation: async (email) => {
    const response = await api.post('/auth/resend-confirmation', { email });
    return response.data;
  },
};

