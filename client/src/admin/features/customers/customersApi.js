import api from "../../../services/api.js";
export const customersApi = {
  getOperations: async (id, signal) =>
    (await api.get(`/admin/customers/${id}/operations`, { signal })).data,
  getAllCustomers: async (params = {}, signal) => {
    const { search, pageNumber = 1, pageSize = 20 } = params;
    const queryParams = new URLSearchParams();
    if (search) queryParams.append("search", search);
    queryParams.append("pageNumber", pageNumber);
    queryParams.append("pageSize", pageSize);

    const response = await api.get(
      `/admin/customers?${queryParams.toString()}`,
      { signal },
    );
    return response.data;
  },

  getCustomerDetails: async (id) => {
    const response = await api.get(`/admin/customers/${id}`);
    return response.data;
  },

  getCustomerOrders: async (id, params = {}) => {
    const { pageNumber = 1, pageSize = 20 } = params;
    const response = await api.get(
      `/admin/customers/${id}/orders?pageNumber=${pageNumber}&pageSize=${pageSize}`,
    );
    return response.data;
  },

  getCustomerStats: async (id) => {
    const response = await api.get(`/admin/customers/${id}/stats`);
    return response.data;
  },
};
