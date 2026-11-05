import { describe, expect, it } from 'vitest';
import { sharedSignInDestination } from './sharedSignInDestination.js';

const site = 'https://store.example.test/Amaara-Creations';
const base = '/Amaara-Creations/';
describe('shared account entry from management', () => {
  it('preserves a permitted management deep link', () => {
    expect(sharedSignInDestination('https://management.example.test/Amaara-Creations/admin/orders?status=paid', site, base)).toBe(site + '/login?returnTo=%2Fadmin%2Forders%3Fstatus%3Dpaid');
  });
  it.each(['register', 'forgot-password', 'reset-password', 'verify-email'])('uses the shared %s page', page => {
    expect(sharedSignInDestination('https://management.example.test/Amaara-Creations/' + page, site, base)).toBe(site + '/' + page);
  });
  it('ignores a forged external destination', () => {
    expect(sharedSignInDestination('https://management.example.test/Amaara-Creations/login?returnTo=//evil.invalid', site, base)).toBe(site + '/login');
  });
  it.each(['javascript:alert(1)', 'https://user:password@store.example.test', 'https://management.example.test', 'https://store.example.test?returnTo=//evil.invalid'])('rejects unsafe shared configuration %s', value => {
    expect(sharedSignInDestination('https://management.example.test/Amaara-Creations/login', value, base)).toBeNull();
  });
});
