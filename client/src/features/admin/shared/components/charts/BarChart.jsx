import { useEffect, useId, useRef, useState } from 'react';
import { Link } from 'react-router-dom';
import { EmptyState } from '../EmptyState.jsx';
import { ErrorState } from '../ErrorState.jsx';
import { ChartTooltip } from './ChartTooltip.jsx';
import { abbreviateNumber, formatMoney } from '@/utils/formatMoney.js';
import { colomboMidnight, COLOMBO } from '@/utils/dateBuckets.js';
import './charts.css';

function dateLabel(date, monthly = false, short = false) {
  return new Intl.DateTimeFormat('en-GB', { timeZone: COLOMBO, ...(monthly ? { month: 'short', ...(short ? {} : { year: 'numeric' }) } : short ? { day: 'numeric' } : { day: '2-digit', month: '2-digit', year: 'numeric' }) }).format(colomboMidnight(date));
}
export function BarChart({ data, previous, monthly = false, loading = false, error, retry, unavailable, canViewOrders = true, days = 30 }) {
  const id = useId().replace(/:/g, '');
  const [active, setActive] = useState(null);
  const [comparison, setComparison] = useState(false);
  const [table, setTable] = useState(false);
  const bars = useRef([]);
  useEffect(() => {
    if (active === null) return;
    const dismiss = event => { if (event.key === 'Escape') setActive(null); };
    document.addEventListener('keydown', dismiss);
    return () => document.removeEventListener('keydown', dismiss);
  }, [active]);
  const total = data?.reduce((sum, bucket) => sum + bucket.value, 0) ?? null;
  const maximum = Math.max(0, ...(data || []).map(bucket => bucket.value), ...(comparison ? previous || [] : []).map(bucket => bucket.value));
  const peak = data?.length ? data.reduce((best, bucket, index) => bucket.value > data[best].value ? index : best, 0) : -1;
  const average = total === null ? null : total / (data?.length || 1);
  const dailyAverage = total === null ? null : total / days;
  const orders = data?.every(bucket => Number.isFinite(bucket.orders)) ? data.reduce((sum, bucket) => sum + bucket.orders, 0) : null;
  const width = Math.max(640, (data?.length || 12) * 34 + 90), height = 224;
  const left = 52, right = width - 18, top = 28, bottom = height - 26;
  const slot = (right - left) / (data?.length || 12), barWidth = Math.min(40, slot * .65);
  const ceiling = maximum ? Math.ceil(maximum / 3 / 10 ** Math.floor(Math.log10(maximum / 3))) * 10 ** Math.floor(Math.log10(maximum / 3)) * 3 : 3;
  const y = value => bottom - value / ceiling * (bottom - top);
  const hasData = total > 0;
  const hasPrevious = previous?.length === data?.length && previous?.some(bucket => bucket.value > 0);
  const summary = loading ? 'Loading sales chart' : hasData ? `Order value ${formatMoney(total)}; peak ${dateLabel(data[peak].date, monthly)}, ${formatMoney(data[peak].value)}; daily average ${formatMoney(dailyAverage)}. A data table is available below.` : unavailable ? 'Sales series unavailable. Outline bars are placeholders, not sales data.' : 'No sales in this period. Outline bars are placeholders, not sales data.';
  const move = (event, index) => {
    const next = event.key === 'ArrowRight' ? Math.min(index + 1, data.length - 1) : event.key === 'ArrowLeft' ? Math.max(0, index - 1) : event.key === 'Home' ? 0 : event.key === 'End' ? data.length - 1 : -1;
    if (next >= 0) { event.preventDefault(); bars.current[next]?.focus(); }
    if (event.key === 'Escape') { event.preventDefault(); setActive(null); }
  };
  if (error) return <div className="sales-chart-state"><ErrorState title="Sales overview could not be loaded" message="Other dashboard cards are still available." retry={retry} /></div>;
  return <>
    <div className={`sales-chart-body ${loading ? 'is-loading' : ''} ${table ? 'is-table-view' : ''}`} aria-busy={loading || undefined} onMouseLeave={() => { if (!bars.current.includes(document.activeElement)) setActive(null); }}>
      <div className="sales-chart-scroll" tabIndex={0} role="region" aria-label="Sales chart">
        {hasData && <span className="admin-sr-only" role="img" aria-label={summary} />}
        <svg className={`sales-bars ${active !== null ? 'has-active' : ''}`} viewBox={`0 0 ${width} ${height}`} role={hasData ? 'group' : 'img'} aria-label={hasData ? 'Interactive sales bars; use left and right arrow keys to move between bars' : summary}>
          <defs><linearGradient id={`${id}-navy`} x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor="var(--navy-600)" /><stop offset="100%" stopColor="var(--navy-800)" /></linearGradient></defs>
          {[0, 1, 2, 3].map(index => <g key={index} className="sales-grid"><line x1={left} x2={right} y1={y(ceiling / 3 * index)} y2={y(ceiling / 3 * index)} /><text x={left - 9} y={y(ceiling / 3 * index) + 4} textAnchor="end">{hasData ? abbreviateNumber(ceiling / 3 * index) : index === 0 ? '0' : ''}</text></g>)}
          {!hasData && Array.from({ length: 12 }, (_, index) => <rect key={index} className={loading ? 'sales-skeleton-bar' : 'sales-ghost-bar'} x={left + (right - left) / 12 * (index + .2)} y={bottom - (32 + ((index * 37 + 23) % 110))} width={(right - left) / 12 * .6} height={32 + ((index * 37 + 23) % 110)} rx="6" aria-hidden="true" />)}
          {hasData && data.map((bucket, index) => {
            const x = left + slot * (index + .5);
            const selected = active === index;
            return <g key={bucket.date} className={`sales-bar-group ${selected ? 'is-active' : ''}`}>
              <rect ref={node => { bars.current[index] = node; }} tabIndex={0} role="button" aria-label={`${dateLabel(bucket.date, monthly)}: ${formatMoney(bucket.value)}, ${bucket.orders ?? 'unavailable'} orders`} aria-describedby={selected ? `${id}-tooltip` : undefined} className={`sales-bar ${index === peak ? 'is-peak' : ''}`} data-stagger={index} x={x - barWidth / 2} y={y(bucket.value)} width={barWidth} height={Math.max(2, bottom - y(bucket.value))} rx="6" fill={`url(#${id}-navy)`} onMouseEnter={() => setActive(index)} onFocus={() => setActive(index)} onBlur={() => setActive(null)} onClick={() => setActive(index)} onKeyDown={event => { if (event.key === 'Enter' || event.key === ' ') { event.preventDefault(); setActive(index); } else move(event, index); }} />
              {index === peak && <text className="sales-peak-label" x={x} y={Math.max(14, y(bucket.value) - 8)} textAnchor="middle">Peak</text>}
              {(index % Math.ceil(data.length / (monthly ? 12 : 10)) === 0 || index === data.length - 1) && <text className="sales-axis-label" x={x} y={height - 5} textAnchor="middle">{dateLabel(bucket.date, monthly, true)}</text>}
            </g>;
          })}
          {hasData && <g className="sales-average"><line x1={left} x2={right} y1={y(average)} y2={y(average)} /><rect x={right - 158} y={y(average) - 21} width="158" height="19" rx="8" /><text x={right - 6} y={y(average) - 7} textAnchor="end">Average {formatMoney(average)}</text></g>}
          {hasData && comparison && hasPrevious && <g className="sales-previous" aria-hidden="true"><polyline points={previous.map((bucket, index) => `${left + slot * (index + .5)},${y(bucket.value)}`).join(' ')} />{previous.map((bucket, index) => <circle key={bucket.date} cx={left + slot * (index + .5)} cy={y(bucket.value)} r="3" />)}</g>}
        </svg>
      </div>
      {loading ? <span className="admin-sr-only" role="status">Loading sales overview</span> : !hasData && <div className="sales-chart-overlay"><EmptyState icon="chart" title={unavailable ? 'Sales history is not available' : 'No sales in this period yet'} description={unavailable ? 'A complete sales history is needed for this date range.' : 'Completed orders will appear here as colourful bars.'} action={canViewOrders && <Link className="btn btn-outline-primary btn-sm" to="/admin/orders">View orders</Link>} /></div>}
      {active !== null && data?.[active] && <ChartTooltip id={`${id}-tooltip`} bucket={data[active]} previous={data[active - 1]} label={dateLabel(data[active].date, monthly)} />}
    </div>
    <div className="sales-chart-controls">
      {!loading && !hasData && !table && <span className="sales-placeholder-label">Outline bars are placeholders · no sales values</span>}
      {hasPrevious && <label><input type="checkbox" checked={comparison} onChange={event => setComparison(event.target.checked)} />Previous period</label>}
      <button className="sales-text-button" type="button" aria-expanded={table} aria-controls={`${id}-table`} onClick={() => setTable(value => !value)}>{table ? 'Hide table' : 'View as table'}</button>
    </div>
    {table && <div id={`${id}-table`} className="sales-data-table"><table><caption>Sales overview data</caption><thead><tr><th scope="col">{monthly ? 'Month' : 'Date'}</th><th scope="col">Order value</th><th scope="col">Orders</th></tr></thead><tbody>{data?.length ? data.map(bucket => <tr key={bucket.date}><td>{dateLabel(bucket.date, monthly)}</td><td>{formatMoney(bucket.value)}</td><td>{bucket.orders ?? '—'}</td></tr>) : <tr><td colSpan="3">{loading ? 'Loading sales data' : 'Sales data is not available for this period.'}</td></tr>}</tbody></table></div>}
    <div className="sales-chart-footer"><div><span>{monthly ? 'Best month' : 'Best day'}</span><strong>{hasData ? dateLabel(data[peak].date, monthly) : '—'}</strong>{hasData && <small>{formatMoney(data[peak].value)}</small>}</div><div><span>Daily average</span><strong>{formatMoney(dailyAverage)}</strong></div><div><span>Orders in period</span><strong>{orders?.toLocaleString('en-GB') ?? '—'}</strong></div></div>
  </>;
}
