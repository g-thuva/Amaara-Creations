export function customerError(error: { response?: { status?: number, data?: unknown } }, fallback = 'We could not complete your request. Please try again.') {
  const status = error?.response?.status;
  if (!status) return 'Unable to connect. Check your connection and try again.';
  if (status === 401) return 'Your session has ended. Please sign in again.';
  if (status === 403) return 'You do not have access to this action.';
  if (status === 404) return 'This item is no longer available.';
  if (status === 409) return 'This item changed. Refresh and try again.';
  if (status === 429) return 'Too many attempts. Please wait a moment and try again.';
  if (status === 400) {
    const data = error.response?.data;
    if (typeof data === 'string') return data || fallback;
    if (data && typeof data === 'object') {
      const errors = 'errors' in data ? data.errors : null;
      if (errors && typeof errors === 'object') {
        const messages = Object.values(errors).flat().filter((value): value is string => typeof value === 'string');
        if (messages.length) return messages.join(' ');
      }
      if ('message' in data && typeof data.message === 'string') return data.message || fallback;
    }
    return fallback;
  }
  return fallback;
}
