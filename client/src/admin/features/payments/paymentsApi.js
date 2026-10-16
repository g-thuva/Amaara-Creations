// @ts-check
import api from "../../../services/api.js";
export const paymentsApi = {
  /**
   * @param {import('./paymentContracts').PaymentFilters} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('./paymentContracts').PaymentList>}
   */
  listPayments: async (params, signal) =>
    (await api.get("/admin/payments", { params, signal })).data,
  /**
   * @param {string | number} id
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('./paymentContracts').Payment>}
   */
  getPayment: async (id, signal) =>
    (await api.get(`/admin/payments/${id}`, { signal })).data,
  /**
   * @param {string | number} id
   * @returns {Promise<import('./paymentContracts').Payment>}
   */
  reconcilePayment: async (id) =>
    (await api.post(`/admin/payments/${id}/reconcile`)).data,
  /**
   * @param {string | number} id
   * @param {import('./paymentContracts').RefundRequest} request
   * @returns {Promise<import('./paymentContracts').Refund>}
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
   * @returns {Promise<import('./paymentContracts').Payment>}
   */
  settlePayment: async (id, idempotencyKey) =>
    (await api.post(`/admin/payments/${id}/settle`, { idempotencyKey })).data,
  /**
   * @param {string | number} id
   * @param {string} idempotencyKey
   * @returns {Promise<import('./paymentContracts').Payment>}
   */
  reverseAuthorization: async (id, idempotencyKey) =>
    (await api.post(`/admin/payments/${id}/cancel`, { idempotencyKey })).data,
};
