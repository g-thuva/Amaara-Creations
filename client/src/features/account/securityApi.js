import api from '@/shared/api/httpClient.js';

export const securityApi = {
  capabilities: async signal => (await api.get('/auth/account/capabilities', { signal })).data,
  requestEmailChange: async data => (await api.post('/auth/account/email-change', data)).data,
  confirmEmailChange: async data => (await api.post('/auth/account/email-change/confirm', data)).data,
  exportAccount: async data => (await api.post('/auth/account/export', data, { responseType: 'blob' })).data,
  requestDeletionReview: async data => (await api.post('/auth/account/deletion-review', data)).data,
  sessions: async () => (await api.get('/auth/sessions')).data,
  revokeSession: async id => api.delete(`/auth/sessions/${encodeURIComponent(id)}`),
  secondFactor: async () => (await api.get('/auth/second-factor')).data,
  beginEnrollment: async data => (await api.post('/auth/second-factor/enrollment', data)).data,
  confirmEnrollment: async data => (await api.post('/auth/second-factor/confirm', data)).data,
  replaceRecoveryCodes: async data => (await api.post('/auth/second-factor/recovery-codes', data)).data,
  disableSecondFactor: async data => (await api.post('/auth/second-factor/disable', data)).data,
};
