import api from "@/shared/api/httpClient.js";
export const builderConfigurationApi = {
  getConfig: async () => (await api.get("/admin/custom-builder")).data,
  updateDraft: async (id, payload) =>
    (await api.put(`/admin/custom-builder/draft/${id}`, payload)).data,
  testQuote: async (id, payload) =>
    (await api.post(`/admin/custom-builder/draft/${id}/quote`, payload)).data,
  publish: async (id) =>
    (await api.post(`/admin/custom-builder/draft/${id}/publish`)).data,
};
