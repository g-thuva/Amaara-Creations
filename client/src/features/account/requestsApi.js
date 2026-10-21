import api from '@/shared/api/httpClient.js';

export const requestsApi = {
  list: async (page, signal) => (await api.get('/account/requests', { params: { page }, signal })).data,
  createQuote: async data => (await api.post('/account/requests/quotes', data)).data,
  cancel: async id => api.post(`/account/requests/${encodeURIComponent(id)}/cancel`),
};
