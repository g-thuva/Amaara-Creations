import { describe, expect, it } from 'vitest';
import { catalogParams, customerError, date, money, safeLink, validQuantity } from './storefront';
import { resolveMediaUrl } from '../services/config';
describe('storefront contracts', () => {
  it('formats server money consistently including zero', () => {
    expect(money(1250)).toContain('1,250.00'); expect(money(0)).toContain('0.00'); expect(money(1250)).toContain('LKR');
  });
  it('uses the Sri Lankan date at UTC day boundaries', () => { expect(date('2026-10-05T20:00:00Z')).toContain('6'); });
  it.each([0, -1, 1.5, NaN, Infinity, 11])('rejects invalid quantity %s', value => { expect(validQuantity(value, 10)).toBe(false); });
  it('accepts the inclusive stock boundary', () => { expect(validQuantity(10, 10)).toBe(true); });
  it.each(['javascript:alert(1)', '//evil.test', '/\\evil.test', 'data:text/html,x', 'java\nscript:alert(1)'])('rejects unsafe CMS links', value => { expect(safeLink(value)).toBeNull(); });
  it('supports internal navigation and configured contact channels', () => { expect(safeLink('/products?categoryId=4')).toBe('/products?categoryId=4'); expect(safeLink('mailto:studio@example.test')).toBeTruthy(); });
  it('preserves URL filters while bounding server pagination', () => {
    expect(catalogParams(new URLSearchParams('search=flower&categoryId=3&collectionId=4&sort=price-asc&inStock=true&page=2'))).toEqual({ search: 'flower', categoryId: '3', collectionId: '4', sort: 'price-asc', inStock: true, pageNumber: 2, pageSize: 12 });
    expect(catalogParams(new URLSearchParams('page=-2&sort=unsupported'))).toEqual({ pageNumber: 1, pageSize: 12, sort: 'newest' });
  });
  it('never exposes server exception bodies', () => { expect(customerError({ response: { status: 500, data: { message: 'SQL password details' } } })).not.toContain('SQL'); });
  it('maps actual validation dictionaries', () => { expect(customerError({ response: { status: 400, data: { errors: { Name: ['Required'] } } } })).toBe('Required'); });
  it('resolves portable media and rejects script URLs', () => { expect(resolveMediaUrl('/uploads/example.png')).toMatch(/\/uploads\/example.png$/); expect(resolveMediaUrl('javascript:alert(1)')).toBe(''); });
});
