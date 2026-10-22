import { describe, expect, it } from 'vitest';
import { loginDestination } from './loginDestination.js';

const local = { development: true, pageUrl: 'http://localhost:5173/Amaara-Creations/login', basePath: '/Amaara-Creations/', managementLoginUrl: '' };
const production = { ...local, development: false, pageUrl: 'https://shop.example.test/Amaara-Creations/login', managementLoginUrl: 'https://staff.example.test/login' };

describe('post-login destinations', () => {
  it.each(['Admin', 'SuperAdmin', 'CatalogManager', 'ProductionStaff', 'CustomerSupport', 'Marketing', 'Analyst'])('sends verified %s membership to the separate local sign-in', role => {
    expect(loginDestination({ roles: ['Customer', role] }, '/checkout', local)).toEqual({ external: true, url: 'http://localhost:5179/Amaara-Creations/login' });
  });
  it('uses all returned roles rather than the first role', () => {
    expect(loginDestination({ role: 'Customer', roles: ['Customer', 'SuperAdmin'] }, '/profile', production)).toEqual({ external: true, url: 'https://staff.example.test/login' });
  });
  it('supports the legacy role field when the roles array is absent', () => {
    expect(loginDestination({ role: 'Admin' }, null, local)?.external).toBe(true);
  });
  it.each([undefined, {}, { roles: [] }, { roles: ['Customer'] }, { roles: ['admin'] }, { roles: [], role: 'Admin' }])('keeps nonstaff identity %j on safe customer paths', user => {
    expect(loginDestination(user, '/checkout?step=delivery', local)).toEqual({ external: false, url: '/checkout?step=delivery' });
    expect(loginDestination(user, 'https://evil.invalid', local)).toEqual({ external: false, url: '/profile' });
  });
  it('uses the current loopback hostname and application base in development', () => {
    expect(loginDestination({ roles: ['Admin'] }, null, { ...local, pageUrl: 'http://127.0.0.1:5173/store/login', basePath: '/store/' })?.url).toBe('http://127.0.0.1:5179/store/login');
  });
  it.each(['', 'http://staff.example.test/login', 'https://localhost:5179/login', 'javascript:alert(1)', '//evil.invalid/login', '/login', 'https://user:password@staff.example.test/login', 'https://staff.example.test/login?token=secret', 'https://staff.example.test/login#token', 'https://staff.example.test/\\login', 'https://staff.example.test/log in', 'https://shop.example.test/other/login'])('refuses unsafe or absent production destination %s', managementLoginUrl => {
    expect(loginDestination({ roles: ['Admin'] }, null, { ...production, managementLoginUrl })).toBeNull();
  });
  it('does not forward the current query, return path or identity to the management URL', () => {
    expect(loginDestination({ roles: ['Admin'], email: 'staff@example.test', token: 'private' }, '//evil.invalid?token=private', { ...production, pageUrl: production.pageUrl + '?returnUrl=https://evil.invalid' })).toEqual({ external: true, url: production.managementLoginUrl });
  });
  it('does not invent a loopback entry for a remotely accessed development storefront', () => {
    expect(loginDestination({ roles: ['Admin'] }, null, { ...local, pageUrl: 'http://192.168.1.2:5173/login' })).toBeNull();
  });
});
