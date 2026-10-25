/** Percent change; a missing or zero baseline has no meaningful percentage. */
export function percentChange(current, previous) {
  if (!Number.isFinite(current) || !Number.isFinite(previous) || previous === 0) return null;
  const change = ((current - previous) / Math.abs(previous)) * 100;
  return Number.isFinite(change) ? Math.round((change + Number.EPSILON) * 10) / 10 : null;
}

/**
 * Non-negative amounts to percentages in tenths. Stable largest-remainder
 * allocation makes the displayed decimal values sum to exactly 100.0%.
 * @param {Array<number | {value: number}>} items
 * @returns {number[]}
 */
export function shareOfTotal(items) {
  const values = items.map(item => typeof item === 'number' ? item : item.value)
    .map(value => Number.isFinite(value) && value > 0 ? value : 0);
  const maximum = Math.max(0, ...values);
  if (!maximum) return values.map(() => 0);
  // Scale first, so even large finite amounts cannot overflow the denominator.
  const scaled = values.map(value => value / maximum);
  const total = scaled.reduce((sum, value) => sum + value, 0);
  const quotas = scaled.map(value => value / total * 1000);
  const tenths = quotas.map(Math.floor);
  const order = quotas.map((value, index) => ({ index, remainder: value - tenths[index] }))
    .sort((a, b) => b.remainder - a.remainder || a.index - b.index);
  const remaining = 1000 - tenths.reduce((sum, value) => sum + value, 0);
  for (let index = 0; index < remaining; index++) tenths[order[index].index]++;
  return tenths.map(value => value / 10);
}

export function formatPercent(value) {
  if (!Number.isFinite(value)) return '—';
  return `${value > 0 ? '+' : value < 0 ? '-' : ''}${Math.abs(value).toFixed(1)}%`;
}
