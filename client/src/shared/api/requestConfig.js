const trimTrailingSlash = (value) => value.replace(/\/+$/, '');

export const API_BASE_URL = trimTrailingSlash(
  import.meta.env.VITE_API_BASE_URL || 'http://localhost:5192/api/v1'
);

export const MEDIA_BASE_URL = trimTrailingSlash(
  import.meta.env.VITE_MEDIA_BASE_URL || API_BASE_URL.replace(/\/api(\/v\d+)?$/i, '')
);

export const resolveMediaUrl = (url) => {
  if (!url) {
    return url;
  }

  if (/^(https?:|blob:)/i.test(url)) {
    return url;
  }

  if (/^[a-z][a-z0-9+.-]*:/i.test(url) || url.startsWith('//') || url.includes('\\')) return '';

  return `${MEDIA_BASE_URL}/${url.replace(/^\/+/, '')}`;
};
