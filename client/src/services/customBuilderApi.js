import api from './api';

export const customBuilderApi = {
  getConfig: async () => (await api.get('/custom-builder/config')).data,
  quote: async (payload, signal) => (await api.post('/custom-builder/quote', payload, { signal })).data,
  listDesigns: async (params = {}) => (await api.get('/custom-designs', { params })).data,
  getDesign: async (id) => (await api.get(`/custom-designs/${id}`)).data,
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

export const adminCustomBuilderApi = {
  getConfig: async () => (await api.get('/admin/custom-builder')).data,
  updateDraft: async (id, payload) => (await api.put(`/admin/custom-builder/draft/${id}`, payload)).data,
  testQuote: async (id, payload) => (await api.post(`/admin/custom-builder/draft/${id}/quote`, payload)).data,
  publish: async (id) => (await api.post(`/admin/custom-builder/draft/${id}/publish`)).data,
  listDesigns: async (params = {}) => (await api.get('/admin/custom-designs', { params })).data,
  getDesign: async (id) => (await api.get(`/admin/custom-designs/${id}`)).data,
  uploadProof: async (id, file, customerVisibleMessage) => {
    const form = new FormData();
    form.append('file', file);
    form.append('customerVisibleMessage', customerVisibleMessage || '');
    return (await api.post(`/admin/custom-designs/${id}/proofs`, form, { headers: { 'Content-Type': 'multipart/form-data' } })).data;
  },
  updateProductionStatus: async (id, status) => (await api.put(`/admin/custom-designs/${id}/production-status`, { status })).data,
};
