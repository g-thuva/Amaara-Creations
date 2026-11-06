import api from "@/shared/api/httpClient.js";
export const settingsApi = {
  getCatalogue: async signal => (await api.get('/admin/settings/catalogue', { signal })).data,
  getSettings: async (signal) => {
    const response = await api.get("/admin/settings", { signal });
    return response.data;
  },

  saveSetting: async (payload) => {
    const response = await api.put(
      `/admin/settings/${encodeURIComponent(payload.key)}`,
      payload,
    );
    return response.data;
  },
};
