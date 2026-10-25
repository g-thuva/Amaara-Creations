import { describe, expect, it } from 'vitest';
import { percentChange, shareOfTotal, formatPercent } from './percent.js';
describe('percentage comparisons', () => {
  it.each([[10, 0], [0, 0], [10, undefined], [NaN, 1], [10, Infinity]])('has no invalid or zero baseline percentage (%s, %s)', (current, previous) => expect(percentChange(current, previous)).toBeNull());
  it.each([[112.4, 100, 12.4], [96.9, 100, -3.1], [100, 100, 0], [1, 3, -66.7], [-5, -10, 50], [-15, -10, -50]])('compares %s with %s as %s', (current, previous, result) => expect(percentChange(current, previous)).toBe(result));
  it('formats signs and a decimal without NaN or Infinity', () => {
    expect(formatPercent(12.4)).toBe('+12.4%'); expect(formatPercent(-3.1)).toBe('-3.1%'); expect(formatPercent(-0)).toBe('0.0%'); expect(formatPercent(null)).toBe('—');
  });
});
describe('largest remainder shares', () => {
  it('allocates tied remainders stably', () => expect(shareOfTotal([1, 1, 1])).toEqual([33.4, 33.3, 33.3]));
  it('handles no sales and rejects invalid negative amounts', () => {
    expect(shareOfTotal([])).toEqual([]); expect(shareOfTotal([0, 0])).toEqual([0, 0]); expect(shareOfTotal([NaN, -1, 5])).toEqual([0, 0, 100]);
  });
  it('does not overflow the denominator', () => expect(shareOfTotal([Number.MAX_VALUE, Number.MAX_VALUE])).toEqual([50, 50]));
  it('sums displayed tenths to 1000 across uneven lists', () => {
    for (let length = 1; length <= 80; length++) {
      const items = Array.from({ length }, (_, index) => ({ value: ((index + 13) * 1949 % 997) / 13 }));
      expect(shareOfTotal(items).reduce((sum, value) => sum + Math.round(value * 10), 0)).toBe(1000);
    }
  });
});
