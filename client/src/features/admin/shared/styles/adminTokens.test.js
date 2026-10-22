import { readFileSync } from 'node:fs';
import { describe, expect, it } from 'vitest';
const source = readFileSync(new URL('./adminTokens.css', import.meta.url), 'utf8');
const tokens = Object.fromEntries([...source.matchAll(/--([\w-]+):\s*(#[0-9A-Fa-f]{6})/g)].map(match => [match[1], match[2]]));
function luminance(hex) { return [1, 3, 5].map(index => parseInt(hex.slice(index, index + 2), 16) / 255).map(value => value <= .04045 ? value / 12.92 : ((value + .055) / 1.055) ** 2.4).reduce((sum, value, index) => sum + value * [.2126, .7152, .0722][index], 0); }
const textPairs = [['text-muted', 'bg-surface'], ['text-muted', 'bg-page'], ['gold-700', 'bg-surface'], ['text-on-navy-muted', 'navy-900'], ['navy-900', 'gold-500'], ...['success', 'warning', 'danger', 'info', 'neutral'].map(tone => [tone + '-text', tone + '-bg'])];
describe('admin colour accessibility', () => {
  it.each(textPairs)('%s text contrasts with %s at AA', (foreground, background) => { const [low, high] = [luminance(tokens[foreground]), luminance(tokens[background])].sort((a, b) => a - b); expect((high + .05) / (low + .05)).toBeGreaterThanOrEqual(4.5); });
  it.each([['control-border', 'bg-surface'], ['gold-500', 'navy-900'], ['gold-700', 'gold-100']])('%s interactive/icon contrast against %s', (foreground, background) => { const [low, high] = [luminance(tokens[foreground]), luminance(tokens[background])].sort((a, b) => a - b); expect((high + .05) / (low + .05)).toBeGreaterThanOrEqual(3); });
});
