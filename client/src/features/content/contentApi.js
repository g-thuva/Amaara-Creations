// @ts-check
import api from '@/shared/api/httpClient.js';
export const contentApi = {
  /** @param {AbortSignal} [signal] @returns {Promise<import('./contentContracts').SiteSetting[]>} */
  settings: async (signal) => (await api.get('/content/settings', { signal })).data,
  /** @param {string} slug @param {AbortSignal} [signal] @returns {Promise<import('./contentContracts').CmsPage|null>} */
  page: async (slug, signal) => {
    try { return (await api.get(`/content/pages/${encodeURIComponent(slug)}`, { signal })).data; }
    catch (error) { if (error && typeof error === 'object' && 'response' in error && error.response && typeof error.response === 'object' && 'status' in error.response && error.response.status === 404) return null; throw error; }
  },
};
