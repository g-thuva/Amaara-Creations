import { describe, expect, it } from 'vitest';
import { loginDestination } from './loginDestination.js';

const options = { pageUrl: 'https://store.example.test/Amaara-Creations/login', managementLoginUrl: 'https://management.example.test/Amaara-Creations/login', development: false };

describe('shared login destination', () => {
  it('uses a safe customer return path', () => {
    const user = { id: 'verified', roles: ['Customer'] };
    expect(loginDestination(user, '/checkout?step=delivery')).toEqual({ external: false, url: '/checkout?step=delivery' });
    expect(loginDestination(user, '//evil.invalid')).toEqual({ external: false, url: '/profile' });
    expect(loginDestination(user, '/admin/dashboard')).toEqual({ external: false, url: '/profile' });
  });
  it.each([null, {}, { id: 'unknown', roles: [] }, { id: 'unknown', roles: ['Unknown'] }])('rejects ineligible or unresolved identity %j', user => {
    expect(loginDestination(user, '/profile')).toBeNull();
  });
  it.each(['Admin', 'SuperAdmin', 'CatalogManager', 'ProductionStaff', 'CustomerSupport', 'Marketing', 'Analyst'])('hands verified %s to the management app', role => {
    expect(loginDestination({ id: 'verified', roles: ['Customer', role] }, '/admin/orders', options)).toEqual({ external: true, url: options.managementLoginUrl + '?returnTo=%2Fadmin%2Forders' });
  });
  it.each(['', 'http://management.example.test/login', 'javascript:alert(1)', 'https://user:password@management.example.test/login', 'https://store.example.test/login', 'https://localhost/login', 'https://management.example.test/login?returnTo=//evil.invalid'])('rejects unsafe management configuration %s', managementLoginUrl => {
    expect(loginDestination({ id: 'verified', roles: ['Admin'] }, '/admin', { ...options, managementLoginUrl })).toBeNull();
  });
  it('ignores a forged external return path', () => {
    expect(loginDestination({ id: 'verified', roles: ['Admin'] }, '//evil.invalid', { ...options, managementReturn: 'https://evil.invalid' })).toEqual({ external: true, url: options.managementLoginUrl });
  });
  it('provides the local management address during development', () => {
    expect(loginDestination({ id: 'verified', roles: ['Admin'] }, undefined, { managementLoginUrl: '', pageUrl: 'http://localhost:5179/Amaara-Creations/login', basePath: '/Amaara-Creations/', development: true })).toEqual({ external: true, url: 'http://localhost:5173/Amaara-Creations/login' });
  });
});
