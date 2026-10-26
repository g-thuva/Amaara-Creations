import { test } from 'node:test';
import assert from 'node:assert/strict';
import http from 'node:http';
import { once } from 'node:events';
import { createWebhookProxy } from './webhook-tunnel-proxy.mjs';

test('webhook ingress preserves exact UTF-8 bytes and signature header', async () => {
  const raw = Buffer.from(' {\r\n "note":"தமிழ் é", "amount":22500 }\r\n', 'utf8');
  const signature = '0'.repeat(64);
  let received;
  const upstream = http.createServer(async (request, response) => {
    const chunks = []; for await (const chunk of request) chunks.push(chunk);
    received = { body: Buffer.concat(chunks), signature: request.headers['x-webhook-signature'] };
    response.writeHead(204).end();
  });
  upstream.listen(0, '127.0.0.1'); await once(upstream, 'listening');
  const proxy = createWebhookProxy(upstream.address().port); proxy.listen(0, '127.0.0.1'); await once(proxy, 'listening');
  try {
    const response = await fetch(`http://127.0.0.1:${proxy.address().port}/api/v1/payments/zahls/webhook`, { method: 'POST', body: raw, headers: { 'X-Webhook-Signature': signature, 'Content-Type': 'application/json' } });
    assert.equal(response.status, 204); assert.deepEqual(received.body, raw); assert.equal(received.signature, signature);
  } finally { proxy.closeAllConnections(); proxy.close(); upstream.closeAllConnections(); upstream.close(); }
});
test('webhook ingress rejects other routes and methods', async () => {
  const proxy = createWebhookProxy(); proxy.listen(0, '127.0.0.1'); await once(proxy, 'listening');
  const origin = `http://127.0.0.1:${proxy.address().port}`;
  try {
    for (const path of ['/swagger/v1/swagger.json', '/simulator/api/session', '/api/v1/auth/sign-in', '/api/v1/admin/payments', '/__fixture/health']) {
      assert.equal((await fetch(origin + path, { method: 'POST' })).status, 404);
    }
    assert.equal((await fetch(origin + '/api/v1/payments/zahls/webhook')).status, 404);
    assert.equal((await fetch(origin + '/api/v1/payments/zahls/webhook?path=/admin', { method: 'POST' })).status, 404);
    assert.equal((await fetch(origin + '/api/v1/payments/datatrans/webhook', { method: 'POST' })).status, 404);
  } finally { proxy.closeAllConnections(); proxy.close(); }
});

test('webhook ingress rejects unsigned, non-JSON and oversized requests before forwarding', async () => {
  const proxy = createWebhookProxy(); proxy.listen(0, '127.0.0.1'); await once(proxy, 'listening');
  const url = `http://127.0.0.1:${proxy.address().port}/api/v1/payments/zahls/webhook`;
  try {
    assert.equal((await fetch(url, { method: 'POST', body: '{}', headers: { 'Content-Type': 'application/json' } })).status, 401);
    assert.equal((await fetch(url, { method: 'POST', body: '{}', headers: { 'X-Webhook-Signature': '0'.repeat(64) } })).status, 415);
    assert.equal((await fetch(url, { method: 'POST', body: 'x'.repeat(65537), headers: { 'Content-Type': 'application/json', 'X-Webhook-Signature': '0'.repeat(64) } })).status, 413);
  } finally { proxy.closeAllConnections(); proxy.close(); }
});
test('legacy ingress remains an explicit provider choice', async () => {
  const proxy = createWebhookProxy(5192, 'Datatrans'); proxy.listen(0, '127.0.0.1'); await once(proxy, 'listening');
  const origin = `http://127.0.0.1:${proxy.address().port}`;
  try {
    assert.equal((await fetch(origin + '/api/v1/payments/zahls/webhook', { method: 'POST' })).status, 404);
  } finally { proxy.closeAllConnections(); proxy.close(); }
});
