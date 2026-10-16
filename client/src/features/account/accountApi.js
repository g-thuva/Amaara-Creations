import api from '@/shared/api/httpClient.js';

export const accountApi = {
  // Get user profile
  getProfile: async () => {
    const response = await api.get('/users/profile');
    return response.data;
  },

  // Update user profile
  updateProfile: async (profileData) => {
    const response = await api.put('/users/profile', profileData);
    return response.data;
  },



  // Update avatar URL
  updateAvatar: async (avatarUrl) => {
    const response = await api.post('/users/profile/avatar', { avatarUrl });
    return response.data;
  },
};

