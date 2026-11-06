import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { pathToFileURL } from 'node:url';
import { gzipSync } from 'node:zlib';

export function measurePerformance(manifest, readAsset) {
  const entries = Object.keys(manifest).filter(key => manifest[key].isEntry);
  assert.equal(entries.length, 1, 'Expected one storefront entry in the build manifest.');
  const entry = entries[0];
  const stylesFor = (key, visited = new Set(), styles = new Set()) => {
    if (visited.has(key)) return styles;
    visited.add(key);
    const chunk = manifest[key];
    assert.ok(chunk, `Missing manifest dependency: ${key}`);
    for (const stylesheet of chunk.css || []) styles.add(stylesheet);
    for (const dependency of chunk.imports || []) stylesFor(dependency, visited, styles);
    return styles;
  };
  const bytes = styles => [...styles].reduce((sum, name) => sum + readAsset(name).length, 0);
  const initialStyles = stylesFor(entry);
  const pages = Object.keys(manifest).filter(key => manifest[key].isEntry || manifest[key].isDynamicEntry);
  const allStyles = new Set(Object.values(manifest).flatMap(chunk => chunk.css || []));
  const routeFiles = new Set(Object.values(manifest).map(chunk => chunk.file).filter(file => file.endsWith('.js') && file !== manifest[entry].file));
  return {
    initialGzipBytes: gzipSync(readAsset(manifest[entry].file)).length, initialBudget: 128000,
    largestRouteGzipBytes: Math.max(0, ...[...routeFiles].map(file => gzipSync(readAsset(file)).length)), routeBudget: 40000,
    initialCssBytes: bytes(initialStyles),
    largestPageCssBytes: Math.max(...pages.map(key => bytes(new Set([...initialStyles, ...stylesFor(key)])))),
    cssBudget: 100000,
    totalCssBytes: bytes(allStyles),
  };
}

export function withinPerformanceBudgets(result) {
  return result.initialGzipBytes <= result.initialBudget
    && result.largestRouteGzipBytes <= result.routeBudget
    && result.initialCssBytes <= result.cssBudget
    && result.largestPageCssBytes <= result.cssBudget;
}

if (process.argv[1] && import.meta.url === pathToFileURL(resolve(process.argv[1])).href) {
  const dist = resolve('dist');
  const manifest = JSON.parse(readFileSync(resolve(dist, '.vite/manifest.json'), 'utf8'));
  const result = measurePerformance(manifest, name => readFileSync(resolve(dist, name)));
  console.log(JSON.stringify(result, null, 2));
  if (!withinPerformanceBudgets(result)) process.exitCode = 1;
}
