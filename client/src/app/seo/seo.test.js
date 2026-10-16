import { describe, it, expect } from 'vitest';
import { seoForRoute } from '@/app/seo/seo.js';
import { productSchema } from '@/features/catalog/productSeo.js';
import { publicBase } from '@/shared/utils/publicBase.js';
import { can, adminStart } from '@/app/config/permissions.js';

describe('Public indexing boundaries', () => {
  it.each(['/admin/orders/1', '/account/designs/1', '/checkout/payment/success', '/orders/1', '/profile', '/custom/2', '/not-a-route'])('excludes private or unknown route %s', route => expect(seoForRoute(route, 'https://store.example.test').index).toBe(false));
  it('uses approved canonical base paths and strips transient query values', () => expect(seoForRoute('/products/12', 'https://store.example.test/shop').canonical).toBe('https://store.example.test/shop/products/12'));
  it.each(['http://unsafe.test', 'https://user:secret@store.test', 'https://store.test/?token=x', 'https://store.test/#/'])('rejects unsafe public base %s', base => expect(publicBase(base)).toBeNull());
  it('does not invent a canonical host or currency', () => { expect(seoForRoute('/', '').canonical).toBeNull(); expect(productSchema({ price: 10 }, 'https://store.example.test')).toBeNull(); });
  it('uses actual product currency and availability without conversion', () => { const schema = productSchema({ id: 1, name: 'CHF fixture', price: 25.25, currency: 'CHF', stock: 0 }, 'https://store.example.test'); expect(schema.offers.priceCurrency).toBe('CHF'); expect(schema.offers.price).toBe(25.25); expect(schema.offers.availability).toContain('OutOfStock'); });
});
describe('Staff navigation boundaries', () => {
  it('directs support to customers and hides money/security operations', () => { const user = { roles: ['CustomerSupport'] }; expect(can(user, 'ViewOrders')).toBe(true); for (const policy of ['RefundPayments', 'ManagePayments', 'ManageStaff', 'ManageSettings', 'ViewAudit']) expect(can(user, policy)).toBe(false); expect(adminStart(user)).toBe('/admin/customers'); });
  it('gives analysts reporting access without mutation controls', () => { expect(can({ roles: ['Analyst'] }, 'ViewReports')).toBe(true); expect(can({ roles: ['Analyst'] }, 'ManageInventory')).toBe(false); });
});
