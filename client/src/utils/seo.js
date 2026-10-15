const pages = {
  '/': ['Custom stickers & decals', 'Custom stickers, decals and personalised printed details from Amaara Creations.'],
  '/products': ['Shop stickers & decals', 'Browse the current Amaara Creations range of stickers, decals and printed products.'],
  '/custom': ['Custom sticker builder', 'Create a custom sticker with dimensions, materials, finishes and private artwork.'],
  '/about': ['About Amaara', 'Learn about Amaara Creations and our custom printed products.'],
  '/contact': ['Contact Amaara', 'Contact Amaara Creations about products and custom printing.'],
};
const privateTitles = { login: 'Sign in', register: 'Create account', cart: 'Shopping cart', checkout: 'Checkout', orders: 'Your orders', profile: 'Your profile', addresses: 'Your addresses', wishlist: 'Wishlist', admin: 'Administration', account: 'Your account', 'forgot-password': 'Reset your password', 'reset-password': 'Reset your password', 'verify-email': 'Verify email' };
export function publicBase(value) {
  if (!value) return null;
  try { const url = new URL(value); return url.protocol === 'https:' && !url.username && !url.password && !url.search && !url.hash ? url.href.replace(/\/+$/, '') : null; } catch { return null; }
}
export function seoForRoute(path, base) {
  const isProduct = /^\/products\/\d+$/.test(path);
  const isPublic = Boolean(pages[path]) || isProduct;
  return { title: pages[path]?.[0] || (isProduct ? 'Product details' : privateTitles[path.split('/')[1]] || 'Page not found'),
    description: pages[path]?.[1] || 'Amaara Creations products and customer services.', index: isPublic,
    canonical: publicBase(base) ? publicBase(base) + (path === '/' ? '/' : path) : null };
}
export function productSchema(product, base) {
  const origin = publicBase(base);
  if (!origin || !product || !Number.isFinite(product.price) || product.price < 0 || !/^[A-Z]{3}$/.test(product.currency || '')) return null;
  return { '@context': 'https://schema.org', '@type': 'Product', name: product.name, description: product.shortDescription || product.description,
    ...(product.baseSku ? { sku: product.baseSku } : {}),
    offers: { '@type': 'Offer', price: product.price, priceCurrency: product.currency, url: origin + '/products/' + product.id,
      availability: product.stock > 0 ? 'https://schema.org/InStock' : 'https://schema.org/OutOfStock' } };
}
