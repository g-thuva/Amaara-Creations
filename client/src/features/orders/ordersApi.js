// @ts-check
import api from '@/shared/api/httpClient.js';

export const ordersApi = {
  /** @param {AbortSignal} [signal] @returns {Promise<import('./orderContracts').Order[]>} */
  getUserOrders: async (signal) => (await api.get('/orders', { signal })).data,
  /** @param {number|string} id @param {AbortSignal} [signal] @returns {Promise<import('./orderContracts').Order>} */
  getOrderById: async (id, signal) => (await api.get(`/orders/${id}`, { signal })).data,
};
