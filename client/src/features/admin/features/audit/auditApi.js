import api from "@/shared/api/httpClient.js";
export const auditApi = {
  listEntries: async (params, signal) =>
    (await api.get("/admin/audit", { params, signal })).data,
};
