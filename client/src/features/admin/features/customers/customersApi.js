import api from "@/shared/api/httpClient.js";
export const customersApi = {
  changeStatus: async (id, value) => (await api.put(`/admin/customers/${id}/status`, value)).data,
  getOperations: async (id, signal) =>
    (await api.get(`/admin/customers/${id}/operations`, { signal })).data,
  getAllCustomers: async (params = {}, signal) => {
    const { search, status, sort, pageNumber = 1, pageSize = 20 } = params;
    const queryParams = new URLSearchParams();
    if (search) queryParams.append("search", search);
    if (status) queryParams.append('status', status);
    if (sort) queryParams.append('sort', sort);
    queryParams.append("pageNumber", pageNumber);
    queryParams.append("pageSize", pageSize);

    const response = await api.get(
      `/admin/customers?${queryParams.toString()}`,
      { signal },
    );
    return response.data;
  },



};
