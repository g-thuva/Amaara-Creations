import api from "@/shared/api/httpClient.js";
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



};
