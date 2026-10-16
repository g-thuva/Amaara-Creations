import api from "@/shared/api/httpClient.js";
export const mediaApi = {
  getMedia: async (params = {}, signal) => {
    const response = await api.get("/admin/media", { params, signal });
    return response.data;
  },
};
