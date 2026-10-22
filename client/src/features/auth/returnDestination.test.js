import { describe, expect, it } from 'vitest';
import { safeReturnDestination } from './returnDestination.js';
describe('application-scoped return destinations', () => {
  it.each(['https://evil.invalid', '//evil.invalid', '/\\evil.invalid', '/%2f%2fevil.invalid', '/login', '/register', '/admin/orders', '/\n/evil.invalid'])('keeps unsafe or management destination %s out of customer sign-in', value => {
    expect(safeReturnDestination(value)).toBe('/profile');
  });
  it('preserves local shopping paths and query state', () => {
    expect(safeReturnDestination('/checkout?step=delivery')).toBe('/checkout?step=delivery');
    expect(safeReturnDestination('/orders/42#proof')).toBe('/orders/42#proof');
  });
  it('allows only management paths in the management application', () => {
    expect(safeReturnDestination('/admin/orders/42', '/admin/dashboard', true)).toBe('/admin/orders/42');
    expect(safeReturnDestination('/profile', '/admin/dashboard', true)).toBe('/admin/dashboard');
    expect(safeReturnDestination('/admin/../login', '/admin/dashboard', true)).toBe('/admin/dashboard');
  });
});
