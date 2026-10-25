import { describe, expect, it } from 'vitest';
import { customerError } from '@/shared/api/apiError.ts';
import { formatDate as date } from '@/shared/utils/formatDate.ts';
import { formatCurrency as money } from '@/shared/utils/formatCurrency.ts';
import { safeLink } from '@/shared/utils/safeLink.ts';
import { validQuantity } from '@/shared/utils/quantity.ts';
import { resolveMediaUrl } from '@/shared/api/requestConfig.js';
describe('storefront contracts', () => {
  it('formats server money consistently including zero', () => {
    expect(money(1250)).toMatch(/^CHF\s1'250\.00$/); expect(money(0)).toMatch(/^CHF\s0\.00$/);
    expect(money(1250, 'LKR')).toBe('Rs. 1,250.00');
    expect(money(25.5, 'CHF')).toContain('CHF');
    expect(money(25.5, 'CHF')).not.toContain('Rs.');
  });
  it('uses the Sri Lankan date at UTC day boundaries', () => { expect(date('2026-10-05T20:00:00Z')).toContain('6'); });
  it.each([0, -1, 1.5, NaN, Infinity, 11])('rejects invalid quantity %s', value => { expect(validQuantity(value, 10)).toBe(false); });
  it('accepts the inclusive stock boundary', () => { expect(validQuantity(10, 10)).toBe(true); });
  it.each(['javascript:alert(1)', '//evil.test', '/\\evil.test', 'data:text/html,x', 'java\nscript:alert(1)'])('rejects unsafe CMS links', value => { expect(safeLink(value)).toBeNull(); });
  it('supports internal navigation and configured contact channels', () => { expect(safeLink('/products?categoryId=4')).toBe('/products?categoryId=4'); expect(safeLink('mailto:studio@example.test')).toBeTruthy(); });
  it('never exposes server exception bodies', () => { expect(customerError({ response: { status: 500, data: { message: 'SQL password details' } } })).not.toContain('SQL'); });
  it('maps actual validation dictionaries', () => { expect(customerError({ response: { status: 400, data: { errors: { Name: ['Required'] } } } })).toBe('Required'); });
  it('resolves portable media and rejects script URLs', () => { expect(resolveMediaUrl('/uploads/example.png')).toMatch(/\/uploads\/example.png$/); expect(resolveMediaUrl('javascript:alert(1)')).toBe(''); });
  it('supports stable public API media and CDN delivery without exposing private storage references', () => {
    expect(resolveMediaUrl('/api/v1/media/public/avatars/abc.png')).toMatch(/\/api\/v1\/media\/public\/avatars\/abc.png$/);
    expect(resolveMediaUrl('https://cdn.example.test/products/image.png')).toBe('https://cdn.example.test/products/image.png');
    expect(resolveMediaUrl('s3:private/custom-artwork/42/secret.png')).toBe('');
  });
});
