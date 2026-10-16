import { useEffect } from 'react';
import { useLocation } from 'react-router-dom';
import { seoForRoute } from '@/app/seo/seo.js';

export default function Seo() {
  const { pathname } = useLocation();
  useEffect(() => {
    const seo = seoForRoute(pathname, import.meta.env.VITE_PUBLIC_SITE_URL);
    document.title = `${seo.title} | Amaara Creations`;
    const meta = (key, value, property = false) => {
      const attribute = property ? 'property' : 'name';
      let element = document.querySelector(`meta[${attribute}="${key}"]`);
      if (!element) { element = document.createElement('meta'); element.setAttribute(attribute, key); document.head.appendChild(element); }
      element.content = value;
    };
    meta('description', seo.description); meta('robots', seo.index ? 'index,follow' : 'noindex,nofollow');
    meta('og:title', seo.title, true); meta('og:description', seo.description, true); meta('og:type', 'website', true);
    const old = document.querySelector('link[rel="canonical"]'); old?.remove();
    if (seo.canonical && seo.index) { const link = document.createElement('link'); link.rel = 'canonical'; link.href = seo.canonical; document.head.appendChild(link); meta('og:url', seo.canonical, true); }
    else document.querySelector('meta[property="og:url"]')?.remove();
    document.getElementById('route-schema')?.remove();
    if (seo.index && seo.canonical) {
      const script = document.createElement('script'); script.id = 'route-schema'; script.type = 'application/ld+json';
      script.textContent = JSON.stringify({ '@context': 'https://schema.org', '@graph': [
        { '@type': 'Organization', name: 'Amaara Creations', url: import.meta.env.VITE_PUBLIC_SITE_URL },
        { '@type': 'BreadcrumbList', itemListElement: [{ '@type': 'ListItem', position: 1, name: 'Home', item: import.meta.env.VITE_PUBLIC_SITE_URL }, ...(pathname === '/' ? [] : [{ '@type': 'ListItem', position: 2, name: seo.title, item: seo.canonical }])] },
      ] }).replace(/</g, '\\u003c'); document.head.appendChild(script);
    }
    window.scrollTo({ top: 0, behavior: 'instant' });
  }, [pathname]);
  return null;
}
