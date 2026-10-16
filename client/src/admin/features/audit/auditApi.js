import api from "../../../services/api.js";
export const auditApi = {
  listEntries: async (params, signal) =>
    (await api.get("/admin/audit", { params, signal })).data,
};
