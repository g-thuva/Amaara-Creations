import { useId, useState } from 'react';
import { shareOfTotal } from '@/utils/percent.js';
import { abbreviateMoney, formatMoney } from '@/utils/formatMoney.js';
import './charts.css';
export function DonutChart({ items = [], label = 'Total sales', emptyText = 'No sales to break down yet' }) {
  const id = useId();
  const [active, setActive] = useState(null);
  const percentages = shareOfTotal(items);
  const total = items.reduce((sum, item) => sum + item.value, 0);
  const circumference = 2 * Math.PI * 72;
  let offset = 0;
  return <div className={`sales-breakdown-content ${active !== null ? 'has-active' : ''}`} tabIndex={0} role="region" aria-label="Sales breakdown chart and legend">
    <svg className="sales-donut" viewBox="0 0 200 200" role={total > 0 ? 'group' : 'img'} aria-labelledby={`${id}-title`}>
      <title id={`${id}-title`}>{total > 0 ? `${label}: ${formatMoney(total)}. ${items.map((item, index) => `${item.label} ${percentages[index].toFixed(1)}%`).join(', ')}` : emptyText}</title>
      <circle className={total > 0 ? 'sales-donut-track' : 'sales-donut-empty'} cx="100" cy="100" r="72" />
      {total > 0 && items.map((item, index) => {
        const length = item.value / total * circumference;
        const position = offset; offset += length;
        const gap = length > 21 ? 21 : Math.min(3, length / 2);
        const geometry = { cx: 100, cy: 100, r: 72, strokeDasharray: `${Math.max(.01, length - gap)} ${circumference}`, strokeDashoffset: -(position + gap / 2), transform: 'rotate(-90 100 100)', strokeLinecap: length > 21 ? 'round' : 'butt' };
        return item.value > 0 && <g key={item.label}>
          <circle {...geometry} className="sales-donut-outline" aria-hidden="true" />
          <circle {...geometry} className={`sales-donut-segment sales-color-${index % 6 + 1} ${active === index ? 'is-active' : ''}`} tabIndex={0} role="button" aria-label={`${item.label}: ${percentages[index].toFixed(1)}%, ${formatMoney(item.value)}`} onMouseEnter={() => setActive(index)} onMouseLeave={() => setActive(null)} onFocus={() => setActive(index)} onBlur={() => setActive(null)} onClick={() => setActive(index)} onKeyDown={event => { if (event.key === 'Escape') setActive(null); else if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setActive(index); } }} />
        </g>;
      })}
      <text className="sales-donut-total" x="100" y="98" textAnchor="middle">{total > 0 ? abbreviateMoney(total) : '—'}</text>
      <text className="sales-donut-label" x="100" y="118" textAnchor="middle">{label}</text>
    </svg>
    {total > 0 ? <ul className="sales-donut-legend">{items.map((item, index) => <li key={item.label}><button type="button" className={`${active === index ? 'is-active' : ''} sales-legend-row`} onMouseEnter={() => setActive(index)} onMouseLeave={() => setActive(null)} onFocus={() => setActive(index)} onBlur={() => setActive(null)} onKeyDown={event => { if (event.key === 'Escape') setActive(null); }} aria-label={`${item.label}: ${percentages[index].toFixed(1)}%, ${formatMoney(item.value)}`}><span className={`sales-legend-dot sales-color-${index % 6 + 1}`} /><span className="sales-legend-name">{item.label}<small>{formatMoney(item.value)}</small></span><strong>{percentages[index].toFixed(1)}%</strong></button></li>)}</ul> : <p className="sales-donut-empty-text">{emptyText}</p>}
  </div>;
}
