import api from './api';
export const contentApi = {
  settings: async (signal) => (await api.get('/content/settings', { signal })).data,
  page: async (slug, signal) => {
    try { return (await api.get(`/content/pages/${encodeURIComponent(slug)}`, { signal })).data; }
    catch (error) { if (error.response?.status === 404) return null; throw error; }
  },
  categories: async (signal) => (await api.get('/catalog/categories', { signal })).data,
  collections: async (signal) => (await api.get('/catalog/collections', { signal })).data,
};
