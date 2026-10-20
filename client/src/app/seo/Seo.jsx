import { useLayoutEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { seoForRoute } from '@/app/seo/seo.js';
import { applySeo } from '@/shared/components/seo/applySeo.js';

export default function Seo() {
  const { pathname } = useLocation();
  useLayoutEffect(() => {
    applySeo(seoForRoute(pathname, import.meta.env.VITE_PUBLIC_SITE_URL), import.meta.env.VITE_PUBLIC_SITE_URL);
    window.scrollTo({ top: 0, behavior: 'instant' });
  }, [pathname]);
  return null;
}
