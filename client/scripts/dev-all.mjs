import process from 'node:process';
import { fileURLToPath } from 'node:url';
import { parseArgs } from 'node:util';
import { createServer } from 'vite';

const { values } = parseArgs({
  options: {
    host: { type: 'string', default: 'localhost' },
    port: { type: 'string', default: '5179' },
    'admin-port': { type: 'string', default: '5173' },
    help: { type: 'boolean', short: 'h' },
  },
});
if (values.help) {
  console.log('Usage: npm run dev -- [--host localhost] [--port 5179] [--admin-port 5173]');
  process.exit(0);
}

const servers = [];
let closing;
function closeAll(code) {
  closing ??= Promise.allSettled(servers.map(server => server.close())).then(() => process.exit(code));
  return closing;
}
process.once('SIGINT', () => { void closeAll(0); });
process.once('SIGTERM', () => { void closeAll(0); });

function port(value) {
  const number = Number(value);
  if (!Number.isInteger(number) || number < 1 || number > 65535) throw new Error('Development ports must be between 1 and 65535.');
  return number;
}

try {
  const storefrontPort = port(values.port);
  const adminPort = port(values['admin-port']);
  if (storefrontPort === adminPort) throw new Error('Storefront and staff ports must be different.');
  const root = fileURLToPath(new URL('../', import.meta.url));
  const admin = await createServer({ root, mode: 'admin', server: { host: values.host, port: adminPort, strictPort: true } });
  servers.push(admin);
  const storefront = await createServer({ root, mode: 'development', server: { host: values.host, port: storefrontPort, strictPort: true } });
  servers.push(storefront);

  // Shared sign-in is available only after its staff destination is listening.
  await admin.listen();
  await storefront.listen();
  console.log('\n[Storefront] Shared customer and staff sign-in');
  storefront.printUrls();
  console.log('\n[Staff workspace] Opens after successful staff MFA');
  admin.printUrls();
  console.log('\nBoth applications are ready. Press Ctrl+C to stop both.\n');
} catch (error) {
  console.error('[Development] Unable to start both applications: ' + error.message);
  await closeAll(1);
}
