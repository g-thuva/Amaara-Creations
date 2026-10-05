import api from './api';

export const addressApi = {
  getAddresses: async () => {
    const response = await api.get('/addresses');
    return response.data;
  },

  createAddress: async (address) => {
    const response = await api.post('/addresses', address);
    return response.data;
  },

  updateAddress: async (id, address) => {
    const response = await api.put(`/addresses/${id}`, address);
    return response.data;
  },

  deleteAddress: async (id) => {
    const response = await api.delete(`/addresses/${id}`);
    return response.data;
  },

  setDefault: async (id) => {
    const response = await api.put(`/addresses/${id}/default`);
    return response.data;
  }
};
