import { test } from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'node:child_process';
import { once } from 'node:events';
import { createServer } from 'node:net';
import { fileURLToPath } from 'node:url';
import { setTimeout as delay } from 'node:timers/promises';

const root = fileURLToPath(new URL('../', import.meta.url));
const script = fileURLToPath(new URL('./dev-all.mjs', import.meta.url));
async function reservePort() {
  const server = createServer();
  server.listen(0, '127.0.0.1');
  await once(server, 'listening');
  return { server, port: server.address().port };
}
async function close(server) {
  if (server.listening) await new Promise((resolve, reject) => server.close(error => error ? reject(error) : resolve()));
}
async function available(port) {
  const server = createServer();
  try { server.listen(port, '127.0.0.1'); await once(server, 'listening'); }
  finally { await close(server); }
}
function launch(shopPort, staffPort) {
  const child = spawn(process.execPath, [script, '--host', '127.0.0.1', '--port', String(shopPort), '--admin-port', String(staffPort)], { cwd: root, stdio: ['ignore', 'pipe', 'pipe'] });
  let output = '';
  child.stdout.on('data', chunk => { output += chunk; });
  child.stderr.on('data', chunk => { output += chunk; });
  return { child, output: () => output };
}
async function stop(child) {
  if (child.exitCode !== null || child.signalCode !== null) return;
  const stopped = once(child, 'exit', { signal: AbortSignal.timeout(10000) });
  child.kill('SIGTERM');
  await stopped;
}
async function page(port, launched) {
  const deadline = Date.now() + 20000;
  while (Date.now() < deadline) {
    if (launched.child.exitCode !== null) throw new Error(launched.output());
    try {
      const response = await fetch('http://127.0.0.1:' + port + '/Amaara-Creations/', { signal: AbortSignal.timeout(1000) });
      if (response.ok) return response.text();
    } catch { /* Wait for Vite to start listening. */ }
    await delay(50);
  }
  throw new Error('Development server did not start: ' + launched.output());
}

test('one development process serves storefront and staff entries and stops both', { timeout: 30000 }, async () => {
  const shop = await reservePort(); const staff = await reservePort();
  await close(shop.server); await close(staff.server);
  const launched = launch(shop.port, staff.port);
  try {
    assert.match(await page(shop.port, launched), /\/src\/main\.jsx/);
    assert.match(await page(staff.port, launched), /\/src\/admin-main\.jsx/);
  } finally { await stop(launched.child); }
  await available(shop.port); await available(staff.port);
});

for (const blocked of ['storefront', 'staff']) {
  test('an occupied ' + blocked + ' port closes the other application', { timeout: 30000 }, async () => {
    const shop = await reservePort(); const staff = await reservePort();
    const blocker = blocked === 'storefront' ? shop : staff;
    const unused = blocked === 'storefront' ? staff : shop;
    await close(unused.server);
    const launched = launch(shop.port, staff.port);
    try {
      const [code] = await once(launched.child, 'exit', { signal: AbortSignal.timeout(20000) });
      assert.equal(code, 1, launched.output());
      assert.match(launched.output(), /Unable to start both applications/);
      await available(unused.port);
    } finally { await stop(launched.child); await close(blocker.server); }
  });
}
