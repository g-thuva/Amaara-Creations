import api from './api';
export const operationsApi = {
  get: async (path, params, signal) => (await api.get('/admin/' + path, { params, signal })).data,
  post: async (path, data) => (await api.post('/admin/' + path, data, data instanceof FormData ? { headers: { 'Content-Type': 'multipart/form-data' } } : undefined)).data,
  put: async (path, data) => (await api.put('/admin/' + path, data)).data,
  exportOrders: async params => (await api.get('/admin/reports/orders.csv', { params, responseType: 'blob' })).data,
};
