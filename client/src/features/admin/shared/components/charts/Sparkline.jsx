export function Sparkline({ values = [] }) {
  if (values.length < 2 || values.some(value => !Number.isFinite(value))) return null;
  const low = Math.min(...values), span = Math.max(...values) - low || 1;
  return <svg className="sales-sparkline" viewBox="0 0 100 28" aria-hidden="true"><polyline points={values.map((value, index) => `${index / (values.length - 1) * 100},${26 - (value - low) / span * 24}`).join(' ')} /></svg>;
}
