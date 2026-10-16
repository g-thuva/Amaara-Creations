import api from "../../../services/api.js";
export const productsApi = {
  getProducts: async (params = {}, signal) => {
    const response = await api.get("/admin/products", { params, signal });
    return response.data;
  },

  getProduct: async (id, signal) => {
    const response = await api.get(`/admin/products/${id}`, { signal });
    return response.data;
  },

  createProduct: async (payload) => {
    const response = await api.post("/admin/products", payload);
    return response.data;
  },

  updateProduct: async (id, payload) => {
    const response = await api.put(`/admin/products/${id}`, payload);
    return response.data;
  },

  archiveProduct: async (id) => {
    const response = await api.post(`/admin/products/${id}/archive`);
    return response.data;
  },

  reactivateProduct: async (id) => {
    const response = await api.post(`/admin/products/${id}/reactivate`);
    return response.data;
  },

  uploadProductMedia: async (id, file, fields = {}) => {
    const formData = new FormData();
    formData.append("file", file);
    Object.entries(fields).forEach(([key, value]) =>
      formData.append(key, value),
    );
    const response = await api.post(`/admin/products/${id}/media`, formData, {
      headers: { "Content-Type": "multipart/form-data" },
    });
    return response.data;
  },

  getProductMedia: async (id, signal) => {
    const response = await api.get(`/admin/products/${id}/media`, { signal });
    return response.data;
  },

  updateProductMedia: async (productId, mediaId, payload) => {
    const response = await api.put(
      `/admin/products/${productId}/media/${mediaId}`,
      payload,
    );
    return response.data;
  },

  getProductVariants: async (id, signal) => {
    const response = await api.get(`/admin/products/${id}/variants`, {
      signal,
    });
    return response.data;
  },

  createProductVariant: async (id, payload) => {
    const response = await api.post(`/admin/products/${id}/variants`, payload);
    return response.data;
  },

  updateProductVariant: async (productId, variantId, payload) => {
    const response = await api.put(
      `/admin/products/${productId}/variants/${variantId}`,
      payload,
    );
    return response.data;
  },
};
