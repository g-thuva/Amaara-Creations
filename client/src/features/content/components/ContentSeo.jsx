import { useEffect } from 'react';
import { applySeo } from '@/shared/components/seo/applySeo.js';
import { resourceSeo } from '@/shared/components/seo/resourceSeo.js';

export default function ContentSeo({ page, path }) {
  useEffect(() => {
    applySeo(resourceSeo(path, import.meta.env.VITE_PUBLIC_SITE_URL, page), import.meta.env.VITE_PUBLIC_SITE_URL);
  }, [page, path]);
  return null;
}
