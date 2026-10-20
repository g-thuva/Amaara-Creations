import { build } from 'vite';
import { mkdtemp, mkdir } from 'node:fs/promises';
import { resolve, dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const client = resolve(dirname(fileURLToPath(import.meta.url)), '..');
const output = resolve(process.argv[2] || '');
if (!process.argv[2]) throw new Error('An explicit release output directory is required.');
const runtime = resolve(client, '../.runtime');
await mkdir(runtime, { recursive: true });
const envDir = await mkdtemp(join(runtime, 'release-empty-env-'));
const origin = new URL(process.env.VITE_PUBLIC_SITE_URL).origin;
await build({
  root: client,
  configFile: join(client, 'vite.config.js'),
  envDir, // Never read a developer's ignored .env or .env.local files.
  mode: 'production',
  build: { outDir: output, emptyOutDir: false, sourcemap: false },
  plugins: [{
    name: 'amaara-browser-parser-origin',
    enforce: 'pre',
    transform(code, id) {
      // These browser dependencies retain a non-network SSR URL-parser base.
      // Bind that exact fallback to the declared public origin for this browser artifact.
      const path = id.replaceAll('\\', '/');
      if (!path.endsWith('/axios/dist/esm/axios.min.js') && !path.includes('/react-router/dist/production/')) return null;
      const transformed = code.replace(/(["'])http:\/\/localhost\1/g, JSON.stringify(origin));
      return transformed === code ? null : { code: transformed, map: null };
    },
  }],
});
