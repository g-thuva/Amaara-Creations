import { publicBase } from '@/shared/utils/publicBase.js';
export function productSchema(product, base) {
  const origin = publicBase(base);
  if (!origin || !product || !Number.isFinite(product.price) || product.price < 0 || !/^[A-Z]{3}$/.test(product.currency || '')) return null;
  return { '@context': 'https://schema.org', '@type': 'Product', name: product.name, description: product.shortDescription || product.description,
    ...(product.baseSku ? { sku: product.baseSku } : {}),
    offers: { '@type': 'Offer', price: product.price, priceCurrency: product.currency, url: origin + '/products/' + product.id,
      availability: product.stock > 0 ? 'https://schema.org/InStock' : 'https://schema.org/OutOfStock' } };
}
