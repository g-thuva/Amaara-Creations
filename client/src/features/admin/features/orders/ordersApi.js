// @ts-check
import api from "@/shared/api/httpClient.js";
export const ordersApi = {
  /**
   * @param {import('@/features/admin/features/orders/orderContracts.ts').OrderFilters} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('@/features/admin/features/orders/orderContracts.ts').OrderList>}
   */
  listOrders: async (params, signal) =>
    (await api.get("/admin/orders", { params, signal })).data,
  /**
   * @param {string | number} id
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('@/features/orders/orderContracts').Order>}
   */
  getOrder: async (id, signal) =>
    (await api.get(`/admin/orders/${id}`, { signal })).data,
  /**
   * @param {string | number} id
   * @param {import('@/features/admin/features/orders/orderContracts.ts').OrderTransition} request
   * @returns {Promise<import('@/features/orders/orderContracts').Order>}
   */
  updateStatus: async (id, request) =>
    (await api.put(`/admin/orders/${id}/status`, request)).data,
};
