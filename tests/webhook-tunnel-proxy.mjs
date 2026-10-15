import http from 'node:http';
import { fileURLToPath } from 'node:url';
import { resolve } from 'node:path';

// Optional loopback ingress: expose only the selected signed provider webhook.
export function createWebhookProxy(upstreamPort = 5192, provider = 'Zahls') {
  if (!['Zahls', 'Datatrans'].includes(provider)) throw new Error('Unsupported webhook provider');
  const callbackPath = provider === 'Zahls' ? '/api/v1/payments/zahls/webhook' : '/api/v1/payments/datatrans/webhook';
  const signatureHeader = provider === 'Zahls' ? 'x-webhook-signature' : 'datatrans-signature';
  return http.createServer((incoming, outgoing) => {
    if (incoming.method !== 'POST' || incoming.url !== callbackPath) {
      outgoing.writeHead(404).end(); incoming.resume(); return;
    }
    const chunks = []; let length = 0;
    incoming.on('data', chunk => {
      length += chunk.length;
      if (length > 65536) { outgoing.writeHead(413).end(); incoming.destroy(); }
      else chunks.push(chunk);
    });
    incoming.on('end', () => {
      if (outgoing.writableEnded) return;
      const raw = Buffer.concat(chunks);
      const headers = { 'content-type': incoming.headers['content-type'] || 'application/json', 'content-length': raw.length };
      if (incoming.headers[signatureHeader]) headers[signatureHeader] = incoming.headers[signatureHeader];
      const request = http.request({ hostname: '127.0.0.1', port: upstreamPort, method: 'POST', path: incoming.url, headers }, response => {
        outgoing.writeHead(response.statusCode, response.headers); response.pipe(outgoing);
      });
      request.setTimeout(15000, () => request.destroy());
      request.on('error', () => { if (!outgoing.headersSent) outgoing.writeHead(502); outgoing.end(); });
      request.end(raw);
    });
    incoming.on('error', () => { if (!outgoing.writableEnded) outgoing.end(); });
  });
}
if (process.argv[1] && resolve(process.argv[1]) === fileURLToPath(import.meta.url)) {
  createWebhookProxy().listen(5199, '127.0.0.1', () => console.log('Zahls webhook-only ingress listening on 127.0.0.1:5199'));
}
