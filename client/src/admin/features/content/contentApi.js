import api from "../../../services/api.js";
export const contentApi = {
  getPages: async (signal) => {
    const response = await api.get("/admin/content/pages", { signal });
    return response.data;
  },

  createPage: async (payload) => {
    const response = await api.post("/admin/content/pages", payload);
    return response.data;
  },

  updatePage: async (id, payload) => {
    const response = await api.put(`/admin/content/pages/${id}`, payload);
    return response.data;
  },
};
