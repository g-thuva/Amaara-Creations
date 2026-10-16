// @ts-check
import api from '@/shared/api/httpClient.js';

export const paymentsApi = {
  /** @param {number|string} id @param {AbortSignal} [signal] @returns {Promise<import('./paymentContracts').Payment>} */
  payment: async (id, signal) => (await api.get(`/payments/${id}`, { signal })).data,
  /** @param {number} orderId @param {string} idempotencyKey @returns {Promise<import('./paymentContracts').PaymentSession>} */
  retry: async (orderId, idempotencyKey) => (await api.post(`/checkout/orders/${orderId}/retry`, { idempotencyKey })).data,
};
