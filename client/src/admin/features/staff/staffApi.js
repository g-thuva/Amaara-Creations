import api from "../../../services/api.js";
export const staffApi = {
  getStaff: async (signal) => (await api.get("/admin/staff", { signal })).data,
  replaceRoles: async (request) =>
    (await api.put("/admin/staff", request)).data,
};
