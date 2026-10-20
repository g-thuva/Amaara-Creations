import { publicBase } from '@/shared/utils/publicBase.js';

export function resourceSeo(path, base, resource) {
  const origin = publicBase(base);
  return { title: resource?.title || resource?.name || 'Page not found',
    description: (resource?.summary || resource?.shortDescription || resource?.description || resource?.title || resource?.name || 'This page is not available.').slice(0, 160),
    index: Boolean(resource), canonical: origin ? origin + path : null };
}
