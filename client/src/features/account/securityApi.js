import api from '@/shared/api/httpClient.js';

export const securityApi = {
  sessions: async () => (await api.get('/auth/sessions')).data,
  revokeSession: async id => api.delete(`/auth/sessions/${encodeURIComponent(id)}`),
  secondFactor: async () => (await api.get('/auth/second-factor')).data,
  beginEnrollment: async data => (await api.post('/auth/second-factor/enrollment', data)).data,
  confirmEnrollment: async data => (await api.post('/auth/second-factor/confirm', data)).data,
  replaceRecoveryCodes: async data => (await api.post('/auth/second-factor/recovery-codes', data)).data,
  disableSecondFactor: async data => (await api.post('/auth/second-factor/disable', data)).data,
};
