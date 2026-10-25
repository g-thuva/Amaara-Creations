import { formatMoney } from '@/utils/formatMoney.js';
import { TrendChip } from './TrendChip.jsx';
export function ChartTooltip({ id, bucket, previous, label }) {
  if (!bucket) return null;
  return <div className="sales-tooltip" role="tooltip" id={id}>
    <strong>{label}</strong><span>Order value <b>{formatMoney(bucket.value)}</b></span>
    <span>Orders <b>{bucket.orders ?? '—'}</b></span>
    <span>vs previous bar <TrendChip current={bucket.value} previous={previous?.value} /></span>
  </div>;
}
