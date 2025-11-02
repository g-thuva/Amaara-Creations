import api from './api';

export const uploadApi = {
  // Upload product image (Admin only)
  uploadProductImage: async (file) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post('/upload/product-image', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },

  // Upload avatar (Authenticated)
  uploadAvatar: async (file) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post('/upload/avatar', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },
};

