import { FiArrowUpRight, FiArrowDownRight, FiMinus } from 'react-icons/fi';
import { percentChange, formatPercent } from '@/utils/percent.js';
export function TrendChip({ current, previous, description = false }) {
  const change = percentChange(current, previous);
  const tone = change > 0 ? 'positive' : change < 0 ? 'negative' : 'neutral';
  const Icon = change > 0 ? FiArrowUpRight : change < 0 ? FiArrowDownRight : FiMinus;
  const text = change === null ? previous === 0 && current > 0 ? 'New' : '—' : formatPercent(change);
  return <span className="sales-trend-line"><span className={`sales-trend sales-trend-${tone}`}><Icon aria-hidden="true" /><span>{text}</span></span>{description && <span className="sales-comparison">vs previous period</span>}</span>;
}
