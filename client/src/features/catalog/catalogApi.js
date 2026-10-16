// @ts-check
import api from '@/shared/api/httpClient.js';

export const catalogApi = {
  /** @param {Record<string, string|number|boolean>} [params] @param {AbortSignal} [signal] @returns {Promise<import('./catalogContracts').ProductList>} */
  getProducts: async (params = {}, signal) => (await api.get('/products', { params, signal })).data,
  /** @param {number|string} id @param {AbortSignal} [signal] @returns {Promise<import('./catalogContracts').StoreProduct>} */
  getProductById: async (id, signal) => (await api.get(`/products/${encodeURIComponent(id)}`, { signal })).data,
  /** @param {AbortSignal} [signal] @returns {Promise<import('./catalogContracts').Category[]>} */
  getCategories: async (signal) => (await api.get('/catalog/categories', { signal })).data,
  /** @param {AbortSignal} [signal] @returns {Promise<import('./catalogContracts').Collection[]>} */
  getCollections: async (signal) => (await api.get('/catalog/collections', { signal })).data,
};
