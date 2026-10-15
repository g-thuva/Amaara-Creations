import { expect, it } from 'vitest';
import { clientErrorEvent } from './clientTelemetry';
it('keeps tokens, form data, messages and unknown scopes out of frontend telemetry', () => {
  const result = clientErrorEvent('private@example.test', { name: 'AxiosError', message: 'private token', config: { headers: { Authorization: 'Bearer private-token' }, data: { password: 'private-test-password' } }, response: { status: 503 } });
  expect(result).toEqual({ event: 'frontend.error', scope: 'unknown', kind: 'AxiosError', status: 503 });
  expect(JSON.stringify(result)).not.toContain('private');
});
