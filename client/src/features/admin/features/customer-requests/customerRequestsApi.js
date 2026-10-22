import api from '@/shared/api/httpClient.js';
export const customerRequestsApi = {
  list: async (kind, page, signal) => (await api.get(`/admin/customer-requests/${kind === 'privacy' ? 'privacy' : 'quotes'}`, { params: { page }, signal })).data,
  review: async (id, data) => (await api.post(`/admin/customer-requests/${encodeURIComponent(id)}/review`, data)).data,
};
