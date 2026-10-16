import api from "@/shared/api/httpClient.js";
export const collectionsApi = {
  getCollections: async (signal) => {
    const response = await api.get("/admin/collections", { signal });
    return response.data;
  },

  createCollection: async (payload) => {
    const response = await api.post("/admin/collections", payload);
    return response.data;
  },

  updateCollection: async (id, payload) => {
    const response = await api.put(`/admin/collections/${id}`, payload);
    return response.data;
  },

  replaceCollectionProducts: async (id, productIds) => {
    const response = await api.put(`/admin/collections/${id}/products`, {
      productIds,
    });
    return response.data;
  },

  archiveCollection: async (id) => {
    const response = await api.post(`/admin/collections/${id}/archive`);
    return response.data;
  },

  reactivateCollection: async (id) => {
    const response = await api.post(`/admin/collections/${id}/reactivate`);
    return response.data;
  },
};
