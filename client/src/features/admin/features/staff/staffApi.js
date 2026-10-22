import api from "@/shared/api/httpClient.js";
export const staffApi = {
  getStaff: async (signal) => (await api.get("/admin/staff", { signal })).data,
  replaceRoles: async (request) =>
    (await api.put("/admin/staff", request)).data,
  approveMfaRecovery: async (request) => (await api.post('/admin/staff/mfa-recovery', request)).data,
};
