import api from "../../../services/api.js";
export const shippingApi = {
  getConfiguration: async (signal) =>
    (await api.get("/admin/shipping", { signal })).data,
  saveZone: async (value) =>
    (
      await (value.id
        ? api.put(`/admin/shipping/zones/${value.id}`, value)
        : api.post("/admin/shipping/zones", value))
    ).data,
  saveMethod: async (value) =>
    (
      await (value.id
        ? api.put(`/admin/shipping/methods/${value.id}`, value)
        : api.post("/admin/shipping/methods", value))
    ).data,
  updateShipment: async (id, request) =>
    (await api.put(`/admin/shipping/orders/${id}/shipment`, request)).data,
};
