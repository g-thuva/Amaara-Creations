// @ts-check
import api from '@/shared/api/httpClient.js';

export const checkoutApi = {
  /** @param {import('./checkoutContracts').CheckoutRequest} request @param {AbortSignal} [signal] @returns {Promise<import('./checkoutContracts').CheckoutQuote>} */
  quote: async (request, signal) => (await api.post('/checkout/quote', request, { signal })).data,
  /** @param {import('./checkoutContracts').CheckoutSessionRequest} request @returns {Promise<import('../payments/paymentContracts').PaymentSession>} */
  session: async (request) => (await api.post('/checkout/session', request)).data,
};
