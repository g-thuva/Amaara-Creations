// @ts-check
import api from "@/shared/api/httpClient.js";
export const paymentsApi = {
  /**
   * @param {import('@/features/admin/features/payments/paymentContracts.ts').PaymentFilters} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('@/features/admin/features/payments/paymentContracts.ts').PaymentList>}
   */
  listPayments: async (params, signal) =>
    (await api.get("/admin/payments", { params, signal })).data,
  /**
   * @param {string | number} id
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('@/features/payments/paymentContracts').Payment>}
   */
  getPayment: async (id, signal) =>
    (await api.get(`/admin/payments/${id}`, { signal })).data,
  /**
   * @param {string | number} id
   * @returns {Promise<import('@/features/payments/paymentContracts').Payment>}
   */
  reconcilePayment: async (id) =>
    (await api.post(`/admin/payments/${id}/reconcile`)).data,
  /**
   * @param {string | number} id
   * @param {import('@/features/admin/features/payments/paymentContracts.ts').RefundRequest} request
   * @returns {Promise<import('@/features/payments/paymentContracts').Refund>}
   */
  refundPayment: async (id, request) =>
    (await api.post(`/admin/payments/${id}/refunds`, request)).data,
  /**
   * @param {string | number} id
   * @returns {Promise<void>}
   */
  reconcileRefund: async (id) =>
    (await api.post(`/admin/refunds/${id}/reconcile`)).data,
  /**
   * @param {string | number} id
   * @param {string} idempotencyKey
   * @returns {Promise<import('@/features/payments/paymentContracts').Payment>}
   */
  settlePayment: async (id, idempotencyKey) =>
    (await api.post(`/admin/payments/${id}/settle`, { idempotencyKey })).data,
  /**
   * @param {string | number} id
   * @param {string} idempotencyKey
   * @returns {Promise<import('@/features/payments/paymentContracts').Payment>}
   */
  reverseAuthorization: async (id, idempotencyKey) =>
    (await api.post(`/admin/payments/${id}/cancel`, { idempotencyKey })).data,
};
