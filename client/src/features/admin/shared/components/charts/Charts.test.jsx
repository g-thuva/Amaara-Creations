import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { MemoryRouter } from 'react-router-dom';
import { BarChart } from './BarChart.jsx';
import { DonutChart } from './DonutChart.jsx';
import { TrendChip } from './TrendChip.jsx';
const render = node => renderToStaticMarkup(<MemoryRouter>{node}</MemoryRouter>);
describe('sales chart states', () => {
  it('labels outline bars as placeholders without fabricated amounts', () => {
    const html = render(<BarChart data={[]} />); expect(html).toContain('No sales in this period yet'); expect(html).toContain('Outline bars are placeholders'); expect(html).toContain('View orders'); expect(html).not.toContain('is-peak');
  });
  it('distinguishes missing data from zero sales', () => {
    const html = render(<BarChart unavailable="Missing history" />); expect(html).toContain('Sales history is not available'); expect(html).not.toContain('No sales in this period yet'); expect(html).toContain('View as table');
  });
  it('shows skeletons without claiming sales are zero while loading', () => {
    const html = render(<BarChart loading />); expect(html).toContain('aria-busy="true"'); expect(html).toContain('sales-skeleton-bar'); expect(html).not.toContain('No sales in this period yet');
  });
  it('shows an independent retry on failure', () => { const html = render(<BarChart error retry={() => {}} />); expect(html).toContain('role="alert"'); expect(html).toContain('Try again'); expect(html).not.toContain('sales-ghost-bar'); });
  it('renders real values, peak, average, focusable bars and no unsupported previous series', () => {
    const html = render(<BarChart days={2} data={[{ date: '2026-10-22', value: 10, orders: 1 }, { date: '2026-10-23', value: 20, orders: 2 }]} />);
    expect(html).toContain('LKR 30.00'); expect(html).toContain('Peak'); expect(html).toContain('Average LKR 15.00'); expect(html.match(/tabindex="0"/g)).toHaveLength(3); expect(html).not.toContain('Previous period');
  });
  it('makes donut percentages accessible and equal to displayed legend values', () => {
    const html = render(<DonutChart items={[{ label: 'Pending', value: 1 }, { label: 'Shipped', value: 1 }, { label: 'Delivered', value: 1 }]} />);
    expect(html).toContain('33.4%'); expect(html).toContain('33.3%'); expect(html).toContain('Pending: 33.4%, LKR 1.00');
  });
  it('shows New without a fabricated percentage and preserves a flat icon', () => {
    expect(render(<TrendChip current={5} previous={0} />)).toContain('New'); expect(render(<TrendChip current={0} previous={0} />)).toContain('sales-trend-neutral'); expect(render(<TrendChip current={1} previous={1} />)).toContain('0.0%');
  });
});
