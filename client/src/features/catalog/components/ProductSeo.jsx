import { useEffect } from 'react';
import { JsonLd } from '@/shared/components/seo/JsonLd.jsx';
import { productSchema } from '@/features/catalog/productSeo.js';
import { applySeo } from '@/shared/components/seo/applySeo.js';
import { resourceSeo } from '@/shared/components/seo/resourceSeo.js';
export function ProductSeo({ product }) {
  useEffect(() => {
    applySeo(resourceSeo(`/products/${product.id}`, import.meta.env.VITE_PUBLIC_SITE_URL, product), import.meta.env.VITE_PUBLIC_SITE_URL);
  }, [product]);
  const base = import.meta.env.VITE_PUBLIC_SITE_URL;
  const schema = productSchema(product, base);
  return schema ? <JsonLd id="product-schema" value={schema} /> : null;
}
