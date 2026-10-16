// @ts-check
import api from "@/shared/api/httpClient.js";
export const productionApi = {
  /**
   * @param {import('@/features/admin/features/production/productionContracts.ts').ProductionFilters} params
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('@/features/admin/features/production/productionContracts.ts').ProductionQueue>}
   */
  listJobs: async (params, signal) =>
    (await api.get("/admin/custom-designs", { params, signal })).data,
  /**
   * @param {string | number} id
   * @param {AbortSignal} [signal]
   * @returns {Promise<import('@/features/admin/features/production/productionContracts.ts').ProductionJob>}
   */
  getJob: async (id, signal) =>
    (await api.get(`/admin/custom-designs/${id}`, { signal })).data,
  /**
   * @param {string | number} id
   * @param {import('@/features/admin/features/production/productionContracts.ts').ProductionTransition} request
   * @returns {Promise<import('@/features/admin/features/production/productionContracts.ts').ProductionDesign>}
   */
  updateProductionStatus: async (id, request) =>
    (await api.put(`/admin/custom-designs/${id}/production-status`, request))
      .data,
  /**
   * @param {string | number} id
   * @param {FormData} request
   * @returns {Promise<import('@/features/admin/features/production/productionContracts.ts').ProofRevision>}
   */
  uploadProof: async (id, request) =>
    (
      await api.post(`/admin/custom-designs/${id}/proofs`, request, {
        headers: { "Content-Type": "multipart/form-data" },
      })
    ).data,
  /**
   * @param {string | number} id
   * @param {import('@/features/admin/features/production/productionContracts.ts').ProductionTransition} request
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
