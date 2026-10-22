import { fileURLToPath } from 'node:url';
import process from 'node:process';

export function paymentSimulatorConfig(command, mode) {
  const enabled = mode === 'simulator' || process.env.AMAARA_LOCAL_PAYMENT_SIMULATOR === '1';
  if (enabled && command !== 'serve') throw new Error('Local payment simulation cannot be built or published. Unset AMAARA_LOCAL_PAYMENT_SIMULATOR and use a normal release mode.');
  if (!enabled) return { plugins: [], aliases: [] };
  return {
    aliases: [{ find: '@/features/payments/paymentNavigation.js', replacement: fileURLToPath(new URL('./paymentNavigation.simulator.js', import.meta.url)) }],
    plugins: [{
      name: 'development-local-payment-simulator', apply: 'serve',
      transformIndexHtml: {
        order: 'post', handler: html => html.replace('<body>', '<body><aside aria-label="Simulated payments" style="background:#fff2b3;color:#332700;padding:12px 20px;font:600 14px system-ui;border-bottom:2px solid #996b00">SIMULATED PAYMENTS — disposable SQL only; no real money or personal/card information. <a style="color:inherit;text-decoration:underline" href="http://127.0.0.1:5208/simulator" target="_blank" rel="noreferrer">Open payment lab</a></aside>'),
      },
    }],
  };
}
