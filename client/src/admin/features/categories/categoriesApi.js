import api from "../../../services/api.js";
export const categoriesApi = {
  getCategories: async (signal) => {
    const response = await api.get("/admin/categories", { signal });
    return response.data;
  },

  createCategory: async (payload) => {
    const response = await api.post("/admin/categories", payload);
    return response.data;
  },

  updateCategory: async (id, payload) => {
    const response = await api.put(`/admin/categories/${id}`, payload);
    return response.data;
  },

  archiveCategory: async (id) => {
    const response = await api.post(`/admin/categories/${id}/archive`);
    return response.data;
  },

  reactivateCategory: async (id) => {
    const response = await api.post(`/admin/categories/${id}/reactivate`);
    return response.data;
  },
};
