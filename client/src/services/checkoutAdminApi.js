import api from './api';
export const checkoutAdminApi = {
  shipping: async (signal) => (await api.get('/admin/shipping', { signal })).data,
  saveZone: async (value) => (await (value.id ? api.put(`/admin/shipping/zones/${value.id}`, value) : api.post('/admin/shipping/zones', value))).data,
  saveMethod: async (value) => (await (value.id ? api.put(`/admin/shipping/methods/${value.id}`, value) : api.post('/admin/shipping/methods', value))).data,
  payment: async (id, signal) => (await api.get(`/admin/payments/${id}`, { signal })).data,
  reconcile: async (id) => (await api.post(`/admin/payments/${id}/reconcile`)).data,
  refund: async (id, request) => (await api.post(`/admin/payments/${id}/refunds`, request)).data,
  reconcileRefund: async (id) => (await api.post(`/admin/refunds/${id}/reconcile`)).data,
  settle: async (id, idempotencyKey) => (await api.post(`/admin/payments/${id}/settle`, { idempotencyKey })).data,
  cancel: async (id, idempotencyKey) => (await api.post(`/admin/payments/${id}/cancel`, { idempotencyKey })).data,
  shipment: async (id, request) => (await api.put(`/admin/shipping/orders/${id}/shipment`, request)).data,
};
