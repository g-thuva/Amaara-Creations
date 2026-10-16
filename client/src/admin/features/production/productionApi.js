// @ts-check
import api from "../../../services/api.js";
export const productionApi = {
  /**
   * @param {import('./productionContracts').ProductionFilters} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('./productionContracts').ProductionQueue>}
   */
  listJobs: async (params, signal) =>
    (await api.get("/admin/custom-designs", { params, signal })).data,
  /**
   * @param {string | number} id
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('./productionContracts').ProductionJob>}
   */
  getJob: async (id, signal) =>
    (await api.get(`/admin/custom-designs/${id}`, { signal })).data,
  /**
   * @param {string | number} id
   * @param {import('./productionContracts').ProductionTransition} request
   * @returns {Promise<import('./productionContracts').ProductionDesign>}
   */
  updateProductionStatus: async (id, request) =>
    (await api.put(`/admin/custom-designs/${id}/production-status`, request))
      .data,
  /**
   * @param {string | number} id
   * @param {FormData} request
   * @returns {Promise<import('./productionContracts').ProofRevision>}
   */
  uploadProof: async (id, request) =>
    (
      await api.post(`/admin/custom-designs/${id}/proofs`, request, {
        headers: { "Content-Type": "multipart/form-data" },
      })
    ).data,
  /**
   * @param {string | number} id
   * @param {import('./productionContracts').ProductionTransition} request
   * @returns {Promise<void>}
   */
  archiveJob: async (id, request) =>
    (await api.post(`/admin/custom-designs/${id}/archive`, request)).data,
  /**
   * @param {string} url
   * @returns {Promise<Blob>}
   */
  downloadAsset: async (url) =>
    (await api.get(url.replace(/^\/api\/v1/, ""), { responseType: "blob" }))
      .data,
};
