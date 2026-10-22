import { describe, expect, it } from 'vitest';
import { sharedSignInDestination } from './sharedSignInDestination.js';
import { loginDestination } from './loginDestination.js';

describe('shared sign-in handoff', () => {
  const base = '/Amaara-Creations/';
  it('preserves an approved operational deep link across shared login', () => {
    const login = sharedSignInDestination('https://staff.example.test/Amaara-Creations/admin/orders?status=new', 'https://shop.example.test/Amaara-Creations', base);
    const target = new URL(login);
    expect(target.pathname).toBe(base + 'login');
    expect(target.searchParams.get('returnTo')).toBe('/admin/orders?status=new');
    expect(loginDestination({ roles: ['Admin'] }, null, {
      development: false, pageUrl: target.href, basePath: base,
      managementLoginUrl: 'https://staff.example.test/Amaara-Creations/login',
      managementReturn: target.searchParams.get('returnTo'),
    })).toEqual({ external: true, url: 'https://staff.example.test/Amaara-Creations/login?returnTo=%2Fadmin%2Forders%3Fstatus%3Dnew' });
  });
  it('uses the verified local ports without forwarding a login loop', () => {
    expect(sharedSignInDestination('http://localhost:5179/Amaara-Creations/login', '', base)).toBe('http://localhost:5173/Amaara-Creations/login');
  });
  it.each(['javascript:alert(1)', 'http://shop.example.test', 'https://user:pass@shop.example.test', 'https://shop.example.test/?token=private', 'https://shop.example.test/#private', 'https://staff.example.test'])('rejects an invalid shared site %s', site => {
    expect(sharedSignInDestination('https://staff.example.test/admin/orders', site, '/')).toBeNull();
  });
  it('requires explicit configuration outside loopback development', () => {
    expect(sharedSignInDestination('https://staff.example.test/admin/orders', '', '/')).toBeNull();
  });
  it.each(['https://evil.invalid', '//evil.invalid', '/admin/../../login', '/admin%2forders', '/profile', '/login'])('does not forward an unsafe management return %s', managementReturn => {
    expect(loginDestination({ roles: ['Admin'] }, null, {
      development: false, pageUrl: 'https://shop.example.test/login', managementLoginUrl: 'https://staff.example.test/login', managementReturn,
    })).toEqual({ external: true, url: 'https://staff.example.test/login' });
  });
});
