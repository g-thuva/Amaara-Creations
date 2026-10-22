import { afterEach, describe, expect, it, vi } from 'vitest';
import { paymentSimulatorConfig } from '../../../dev/paymentSimulatorPlugin.js';
import { redirectToPayment } from '../../../dev/paymentNavigation.simulator.js';

afterEach(() => vi.unstubAllEnvs());
describe('local payment simulator boundary', () => {
  it('never activates for an ordinary production build', () => {
    vi.stubEnv('AMAARA_LOCAL_PAYMENT_SIMULATOR', '');
    expect(paymentSimulatorConfig('build', 'production')).toEqual({ plugins: [], aliases: [] });
  });
  it.each(['simulator', 'admin', 'production'])('refuses to build enabled simulation in %s mode', mode => {
    vi.stubEnv('AMAARA_LOCAL_PAYMENT_SIMULATOR', '1');
    expect(() => paymentSimulatorConfig('build', mode)).toThrow(/cannot be built/);
  });
  it('maps only the reserved synthetic payment address to the fixed loopback lab', () => {
    const navigate = vi.fn(); redirectToPayment('https://payments.simulator.invalid/pay/123456789012345678', navigate);
    expect(navigate).toHaveBeenCalledWith('http://127.0.0.1:5208/simulator/pay/123456789012345678');
  });
  it.each(['https://pay.zahls.ch/pay/123456789012', 'http://payments.simulator.invalid/pay/123456789012', 'https://payments.simulator.invalid.evil.test/pay/123456789012', 'https://user:secret@payments.simulator.invalid/pay/123456789012', 'https://payments.simulator.invalid/pay/123456789012?url=https://evil.test', 'https://payments.simulator.invalid/pay/123456789012#token', 'http://127.0.0.1:5208/simulator/pay/123456789012'])('refuses any other redirect %s', url => {
    expect(() => redirectToPayment(url, vi.fn())).toThrow();
  });
});
