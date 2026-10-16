import api from "../../../services/api.js";
export const promotionsApi = {
  listPromotions: async (params, signal) =>
    (await api.get("/admin/promotions", { params, signal })).data,
  getRedemptions: async (id, signal) =>
    (await api.get(`/admin/promotions/${id}/redemptions`, { signal })).data,
  createPromotion: async (request) =>
    (await api.post("/admin/promotions", request)).data,
  updatePromotion: async (id, request) =>
    (await api.put(`/admin/promotions/${id}`, request)).data,
};
