import { readFile, readdir } from 'node:fs/promises';
import assert from 'node:assert/strict';

const manifest = JSON.parse(await readFile(new URL('../dist/.vite/manifest.json', import.meta.url), 'utf8'));
const forbiddenModules = Object.keys(manifest).filter(key => /features\/admin|AdminRoutes|admin-main|AdminGuard/.test(key));
assert.deepEqual(forbiddenModules, [], 'Staff modules must never appear in the customer build manifest');
const assets = await readdir(new URL('../dist/assets/', import.meta.url));
for (const asset of assets) {
  assert.ok(!asset.endsWith('.map'), 'Customer source maps must not be published');
  if (!asset.endsWith('.js')) continue;
  const content = await readFile(new URL(`../dist/assets/${asset}`, import.meta.url), 'utf8');
  for (const fragment of ['/admin/capabilities', '/admin/staff', '/admin/payments', 'Admin panel', 'Staff permissions', 'Admin audit log'])
    assert.ok(!content.includes(fragment), `${asset} includes staff-only marker ${fragment}`);
}
console.log(`Storefront isolation passed (${Object.keys(manifest).length} manifest entries, ${assets.length} assets).`);
