import { publicBase } from '@/shared/utils/publicBase.js';

export function applySeo(seo, base) {
  document.title = `${seo.title} | Amaara Creations`;
  const meta = (key, value, property = false) => {
    const attribute = property ? 'property' : 'name';
    let element = document.querySelector(`meta[${attribute}="${key}"]`);
    if (!element) { element = document.createElement('meta'); element.setAttribute(attribute, key); document.head.appendChild(element); }
    element.content = value;
  };
  meta('description', seo.description);
  meta('robots', seo.index ? 'index,follow' : 'noindex,nofollow');
  meta('og:title', seo.title, true);
  meta('og:description', seo.description, true);
  meta('og:type', 'website', true);
  document.querySelector('link[rel="canonical"]')?.remove();
  document.querySelector('meta[property="og:url"]')?.remove();
  document.getElementById('route-schema')?.remove();
  const origin = publicBase(base);
  if (!seo.index || !seo.canonical || !origin) return;
  const link = document.createElement('link'); link.rel = 'canonical'; link.href = seo.canonical; document.head.appendChild(link);
  meta('og:url', seo.canonical, true);
  const script = document.createElement('script'); script.id = 'route-schema'; script.type = 'application/ld+json';
  script.textContent = JSON.stringify({ '@context': 'https://schema.org', '@graph': [
    { '@type': 'Organization', name: 'Amaara Creations', url: origin },
    { '@type': 'BreadcrumbList', itemListElement: [{ '@type': 'ListItem', position: 1, name: 'Home', item: origin + '/' },
      ...(seo.canonical === origin + '/' ? [] : [{ '@type': 'ListItem', position: 2, name: seo.title, item: seo.canonical }])] },
  ] }).replace(/</g, '\\u003c');
  document.head.appendChild(script);
}
