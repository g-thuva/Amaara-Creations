// @ts-check
import api from "@/shared/api/httpClient.js";
// Movements currently expose totalCount with a fixed 50-row server window.
export const movementPageSize = 50;
export const inventoryApi = {
  /**
   * @param {{ page?: number, search?: string, pageSize?: number }} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('@/features/admin/features/inventory/inventoryContracts.ts').BalancePage>}
   */
  getBalances: async (params, signal) =>
    (await api.get("/admin/inventory", { params, signal })).data,
  /**
   * @param {{ page?: number, productId?: number }} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('@/features/admin/features/inventory/inventoryContracts.ts').MovementPage>}
   */
  getMovements: async (params, signal) =>
    (await api.get("/admin/inventory/movements", { params, signal })).data,
  /**
   * @param {number} productId
   * @param {import('@/features/admin/features/inventory/inventoryContracts.ts').InventoryAdjustment} request
   * @returns {Promise<void>}
   */
  adjustStock: async (productId, request) =>
    (await api.post(`/admin/inventory/${productId}/adjustments`, request)).data,
};
