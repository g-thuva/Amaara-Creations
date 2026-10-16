import { expect, it } from 'vitest';
import { customerError } from '@/shared/api/apiError.ts';
it('keeps server diagnostics private while exposing actionable validation', () => {
  expect(customerError({ response: { status: 500, data: { message: 'private storage path' } } })).not.toContain('private');
  expect(customerError({ response: { status: 400, data: { errors: { name: ['Name is required'] } } } })).toBe('Name is required');
});
it('explains expired sessions and optimistic concurrency conflicts', () => {
  expect(customerError({ response: { status: 401 } })).toContain('sign in');
  expect(customerError({ response: { status: 409 } })).toContain('Refresh');
});
it('supports validation strings and safely ignores malformed validation payloads', () => {
  expect(customerError({ response: { status: 400, data: 'Email is already registered.' } })).toBe('Email is already registered.');
  expect(customerError({ response: { status: 400, data: { errors: { name: [null, { private: 'diagnostics' }] } } } }, 'Check your details.')).toBe('Check your details.');
});
