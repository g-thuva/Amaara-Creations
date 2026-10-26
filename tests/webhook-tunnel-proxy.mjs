import http from 'node:http';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

// Optional loopback ingress: expose only the selected signed provider webhook.
export function createWebhookProxy(upstreamPort = 5192, provider = 'Zahls') {
  if (!['Zahls', 'Datatrans'].includes(provider)) throw new Error('Unsupported webhook provider');
  const callbackPath = provider === 'Zahls' ? '/api/v1/payments/zahls/webhook' : '/api/v1/payments/datatrans/webhook';
  const signatureHeader = provider === 'Zahls' ? 'x-webhook-signature' : 'datatrans-signature';
  const server = http.createServer((incoming, outgoing) => {
    if (incoming.method !== 'POST' || incoming.url !== callbackPath) {
      outgoing.writeHead(404).end(); incoming.resume(); return;
    }
    if (provider === 'Zahls') {
      if ((incoming.headers['content-type'] || '').split(';')[0].trim().toLowerCase() !== 'application/json') {
        outgoing.writeHead(415).end(); incoming.resume(); return;
      }
      // Validate shape here; only the backend can authenticate the exact bytes.
      if (!/^[0-9a-f]{64}$/.test(incoming.headers[signatureHeader] || '')) {
        outgoing.writeHead(401).end(); incoming.resume(); return;
      }
    }
    const chunks = []; let length = 0;
    const bodyDeadline = setTimeout(() => { if (!outgoing.writableEnded) outgoing.writeHead(408).end(); incoming.destroy(); }, 10000);
    incoming.on('data', chunk => {
      length += chunk.length;
      if (length > 65536) { if (!outgoing.writableEnded) outgoing.writeHead(413).end(); }
      else if (!outgoing.writableEnded) chunks.push(chunk);
    });
    incoming.on('end', () => {
      clearTimeout(bodyDeadline);
      if (outgoing.writableEnded) return;
      const raw = Buffer.concat(chunks);
      const headers = { 'content-type': incoming.headers['content-type'] || 'application/json', 'content-length': raw.length };
      if (incoming.headers[signatureHeader]) headers[signatureHeader] = incoming.headers[signatureHeader];
      const request = http.request({ hostname: '127.0.0.1', port: upstreamPort, method: 'POST', path: incoming.url, headers }, response => {
        outgoing.writeHead(response.statusCode, response.headers); response.pipe(outgoing);
      });
      const deadline = setTimeout(() => request.destroy(), 15000);
      outgoing.on('close', () => { clearTimeout(deadline); request.destroy(); });
      request.on('close', () => clearTimeout(deadline));
      request.on('error', () => { if (!outgoing.headersSent) outgoing.writeHead(502); outgoing.end(); });
      request.end(raw);
    });
    incoming.on('error', () => { clearTimeout(bodyDeadline); if (!outgoing.writableEnded) outgoing.end(); });
  });
  server.headersTimeout = 10000;
  server.requestTimeout = 10000;
  return server;
}
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  createWebhookProxy().listen(5199, '127.0.0.1', () => console.log('Zahls webhook-only ingress listening on 127.0.0.1:5199'));
}
