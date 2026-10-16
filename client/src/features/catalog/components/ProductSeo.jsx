import { useEffect } from 'react';
import { JsonLd } from '@/shared/components/seo/JsonLd.jsx';
import { productSchema } from '@/features/catalog/productSeo.js';
export function ProductSeo({ product }) {
  useEffect(() => {
    document.title = `${product.name} | Amaara Creations`;
    const description = (product.shortDescription || product.description || product.name).slice(0, 160);
    document.querySelector('meta[name="description"]')?.setAttribute('content', description);
    document.querySelector('meta[property="og:title"]')?.setAttribute('content', product.name);
    document.querySelector('meta[property="og:description"]')?.setAttribute('content', description);
  }, [product]);
  const base = import.meta.env.VITE_PUBLIC_SITE_URL;
  const schema = productSchema(product, base);
  return schema ? <JsonLd id="product-schema" value={schema} /> : null;
}
