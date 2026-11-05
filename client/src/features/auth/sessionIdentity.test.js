import { describe, expect, it } from 'vitest';
import { acceptsIdentity, privilegedRoles } from './sessionIdentity.js';

describe('authentication identity scope', () => {
  it.each(privilegedRoles)('restricts %s including mixed customer membership to management', role => {
    for (const roles of [[role], ['Customer', role], ['Admin', role]]) {
      expect(acceptsIdentity({ id: 'verified', roles })).toBe(false);
      expect(acceptsIdentity({ id: 'verified', roles }, true)).toBe(true);
    }
  });
  it('accepts customer identities only for the storefront', () => {
    expect(acceptsIdentity({ id: 'verified', roles: ['Customer'] })).toBe(true);
    expect(acceptsIdentity({ id: 'verified', roles: ['Customer'] }, true)).toBe(false);
  });
  it.each([null, {}, { id: 'unknown', roles: [] }, { id: 'unknown', roles: ['Unknown'] }, { roles: ['Customer'] }])('fails closed for unresolved identity %j', user => {
    expect(acceptsIdentity(user)).toBe(false);
    expect(acceptsIdentity(user, true)).toBe(false);
  });
});
