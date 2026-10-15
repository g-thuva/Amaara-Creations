const scopes = new Set(['auth.logout', 'storefront.render', 'admin.dashboard']);
export function clientErrorEvent(scope, error) {
  return { event: 'frontend.error', scope: scopes.has(scope) ? scope : 'unknown',
    status: Number.isInteger(error?.response?.status) ? error.response.status : undefined,
    kind: ['Error', 'AxiosError', 'TypeError', 'RangeError'].includes(error?.name) ? error.name : 'Error' };
}
export function reportClientError(scope, error) { console.error(clientErrorEvent(scope, error)); }
