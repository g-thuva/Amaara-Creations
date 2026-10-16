// @ts-check
import api from "../../../services/api.js";
export const ordersApi = {
  /**
   * @param {import('./orderContracts').OrderFilters} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('./orderContracts').OrderList>}
   */
  listOrders: async (params, signal) =>
    (await api.get("/admin/orders", { params, signal })).data,
  /**
   * @param {string | number} id
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('./orderContracts').Order>}
   */
  getOrder: async (id, signal) =>
    (await api.get(`/admin/orders/${id}`, { signal })).data,
  /**
   * @param {string | number} id
   * @param {import('./orderContracts').OrderTransition} request
   * @returns {Promise<import('./orderContracts').Order>}
   */
  updateStatus: async (id, request) =>
    (await api.put(`/admin/orders/${id}/status`, request)).data,
};
