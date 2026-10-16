// @ts-check
import api from "../../../services/api.js";
// Movements currently expose totalCount with a fixed 50-row server window.
export const movementPageSize = 50;
export const inventoryApi = {
  /**
   * @param {{ page?: number, search?: string, pageSize?: number }} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('./inventoryContracts').BalancePage>}
   */
  getBalances: async (params, signal) =>
    (await api.get("/admin/inventory", { params, signal })).data,
  /**
   * @param {{ page?: number, productId?: number }} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('./inventoryContracts').MovementPage>}
   */
  getMovements: async (params, signal) =>
    (await api.get("/admin/inventory/movements", { params, signal })).data,
  /**
   * @param {number} productId
   * @param {import('./inventoryContracts').InventoryAdjustment} request
   * @returns {Promise<void>}
   */
  adjustStock: async (productId, request) =>
    (await api.post(`/admin/inventory/${productId}/adjustments`, request)).data,
};
