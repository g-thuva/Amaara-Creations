import api from "../../../services/api.js";
export const settingsApi = {
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
