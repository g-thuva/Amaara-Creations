import api from "@/shared/api/httpClient.js";
export const reviewsApi = {
  history: async (id, page, signal) => (await api.get(`/admin/reviews/${id}/history`, { params: { page }, signal })).data,
  listReviews: async (params, signal) =>
    (await api.get("/admin/reviews", { params, signal })).data,
  moderateReview: async (id, request) =>
    (await api.put(`/admin/reviews/${id}/moderation`, request)).data,
};
