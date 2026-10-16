import api from "../../../services/api.js";
export const reviewsApi = {
  listReviews: async (params, signal) =>
    (await api.get("/admin/reviews", { params, signal })).data,
  moderateReview: async (id, request) =>
    (await api.put(`/admin/reviews/${id}/moderation`, request)).data,
};
