import api from '@/shared/api/httpClient.js';

export const avatarApi = {
  // Upload avatar (Authenticated)
  uploadAvatar: async (file, onUploadProgress) => {
    const formData = new FormData();
    formData.append('file', file);

    const response = await api.post('/upload/avatar', formData, {
      onUploadProgress,
      headers: {
        'Content-Type': 'multipart/form-data',
      },
    });
    return response.data;
  },
};

