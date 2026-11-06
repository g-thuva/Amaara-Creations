import assert from 'node:assert/strict';
import { test } from 'node:test';
import { measurePerformance, withinPerformanceBudgets } from './check-performance.mjs';

function fixture(routeBytes = 20000) {
  const manifest = {
    entry: { isEntry: true, file: 'entry.js', css: ['base.css'], imports: ['shared'] },
    shared: { file: 'shared.js', css: ['shared.css'], imports: ['entry'] },
    first: { isDynamicEntry: true, file: 'first.js', css: ['first.css'], imports: ['shared'] },
    second: { isDynamicEntry: true, file: 'second.js', css: ['second.css'], imports: ['shared'] },
  };
  const sizes = { 'base.css': 75000, 'shared.css': 5000, 'first.css': routeBytes, 'second.css': 20000 };
  return { manifest, read: name => Buffer.alloc(sizes[name] ?? 100, 'x') };
}

test('CSS budgets count shared imports once and keep unrelated route styles separate', () => {
  const { manifest, read } = fixture();
  const result = measurePerformance(manifest, read);
  assert.equal(result.initialCssBytes, 80000);
  assert.equal(result.largestPageCssBytes, 100000);
  assert.equal(result.totalCssBytes, 120000);
  assert.equal(withinPerformanceBudgets(result), true);
});

test('a route that takes its page over the existing CSS budget fails', () => {
  const { manifest, read } = fixture(20001);
  const result = measurePerformance(manifest, read);
  assert.equal(result.largestPageCssBytes, 100001);
  assert.equal(withinPerformanceBudgets(result), false);
});

test('an oversized common entry fails for every page', () => {
  const { manifest, read } = fixture();
  const result = measurePerformance(manifest, name => name === 'base.css' ? Buffer.alloc(100001) : read(name));
  assert.equal(withinPerformanceBudgets(result), false);
});

test('missing dependencies and ambiguous build entries are rejected', () => {
  const { manifest, read } = fixture();
  assert.throws(() => measurePerformance({ ...manifest, shared: undefined }, read));
  assert.throws(() => measurePerformance({ ...manifest, duplicate: { isEntry: true, file: 'duplicate.js' } }, read));
});
