import api from '@/shared/api/httpClient.js';

export const customDesignApi = {
  getConfig: async (signal) => (await api.get('/custom-builder/config', { signal })).data,
  quote: async (payload, signal) => (await api.post('/custom-builder/quote', payload, { signal })).data,
  listDesigns: async (params = {}) => (await api.get('/custom-designs', { params })).data,
  getDesign: async (id, signal) => (await api.get(`/custom-designs/${id}`, { signal })).data,
  createDesign: async (payload) => (await api.post('/custom-designs', payload)).data,
  updateDesign: async (id, payload) => (await api.put(`/custom-designs/${id}`, payload)).data,
  duplicateDesign: async (id) => (await api.post(`/custom-designs/${id}/duplicate`)).data,
  archiveDesign: async (id) => api.delete(`/custom-designs/${id}`),
  addToCart: async (id) => (await api.post(`/custom-designs/${id}/cart`)).data,
  uploadArtwork: async (id, file) => {
    const form = new FormData();
    form.append('file', file);
    return (await api.post(`/custom-designs/${id}/artwork`, form, { headers: { 'Content-Type': 'multipart/form-data' } })).data;
  },
  deleteArtwork: async (designId, assetId) => api.delete(`/custom-designs/${designId}/assets/${assetId}`),
  downloadPrivate: async (url) => (await api.get(url.replace(/^\/api\/v1/, ''), { responseType: 'blob' })).data,
  approveProof: async (id, revisionNumber, comment) => (await api.post(`/custom-designs/${id}/proofs/approve`, { revisionNumber, comment })).data,
  requestProofChanges: async (id, revisionNumber, comment) => (await api.post(`/custom-designs/${id}/proofs/request-changes`, { revisionNumber, comment })).data,
};
