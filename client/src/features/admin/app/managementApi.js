import api from '@/shared/api/httpClient.js';
export const managementApi = {
  session: async signal => (await api.get('/auth/staff/session', { signal })).data,
};
