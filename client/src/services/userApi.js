import api from './api';

export const userApi = {
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

  // Get user statistics
  getUserStats: async () => {
    const response = await api.get('/users/profile/stats');
    return response.data;
  },

  // Get avatar URL
  getAvatar: async () => {
    const response = await api.get('/users/profile/avatar');
    return response.data;
  },

  // Update avatar URL
  updateAvatar: async (avatarUrl) => {
    const response = await api.post('/users/profile/avatar', { avatarUrl });
    return response.data;
  },
};

