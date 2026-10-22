import { describe, expect, it } from 'vitest';
import { renderToStaticMarkup } from 'react-dom/server';
import { StatusChip } from './StatusChip.jsx';
import { DataTable } from './DataTable.jsx';
import { ConfirmDialog } from './ConfirmDialog.jsx';
import { Field, Input } from './AdminUi.jsx';
import { formatDateTime, formatLocalDate, formatIsoCurrency } from '../utils/format.js';
const render = node => renderToStaticMarkup(node);
const columns = [{ key: 'id', label: 'Identifier' }, { key: 'status', label: 'State' }];
describe('admin state and interaction primitives', () => {
  it.each([['Paid', 'success'], ['Pending', 'warning'], ['Failed', 'danger'], ['Refunded', 'info'], ['Archived', 'neutral'], ['ProviderSpecificState', 'neutral']])('labels %s with an icon and %s tone', (status, tone) => {
    const html = render(<StatusChip status={status} />); expect(html).toContain(status); expect(html).toContain('badge-' + tone); expect(html).toContain('<svg'); expect(html).toContain('aria-hidden="true"');
  });
  it('shows an honest empty state without fake rows', () => {
    const html = render(<DataTable caption="Payments" columns={columns} rows={[]} empty="No payments yet" />);
    expect(html).toContain('No payments yet'); expect(html).toContain('<caption>Payments</caption>'); expect(html).not.toContain('badge-success');
  });
  it('shows skeletons without an empty or success state while loading', () => {
    const html = render(<DataTable caption="Payments" columns={columns} loading />);
    expect(html).toContain('aria-busy="true"'); expect(html).toContain('admin-skeleton'); expect(html).not.toContain('No records found.');
  });
  it('shows retry after failure and retains known rows during background refresh', () => {
    expect(render(<DataTable caption="Payments" columns={columns} error="Network unavailable" retry={() => {}} />)).toContain('Try again');
    const html = render(<DataTable caption="Payments" columns={columns} loading rows={[{ id: 'actual-record', status: 'Paid' }]} />);
    expect(html).toContain('actual-record'); expect(html).toContain('Paid');
  });
  it('does not imply sorting unless the caller exposes it', () => {
    expect(render(<DataTable caption="Payments" columns={columns} />)).not.toContain('admin-sort');
  });
  it('blocks both confirmation actions while pending and preserves the dialog name', () => {
    const html = render(<ConfirmDialog open title="Confirm staff permissions" pending onConfirm={() => {}} onClose={() => {}}>Review existing roles</ConfirmDialog>);
    expect(html).toContain('Confirm staff permissions'); expect(html).toContain('aria-labelledby'); expect(html.match(/disabled=""/g)).toHaveLength(2); expect(html).toContain('aria-busy="true"');
  });
  it('associates field labels, hints and errors with their input', () => {
    const html = render(<Field id="reason" label="Reason" hint="Explain the change" error="A reason is required"><Input /></Field>);
    expect(html).toContain('for="reason"'); expect(html).toContain('aria-describedby="reason-hint reason-error"'); expect(html).toContain('aria-invalid="true"');
  });
});
describe('admin presentation formats', () => {
  it('labels LKR with grouping and two decimals without converting CHF', () => {
    expect(formatIsoCurrency(1234.5, 'LKR')).toBe('LKR 1,234.50'); expect(formatIsoCurrency(1234.5, 'CHF')).toBe('CHF 1,234.50');
  });
  it('renders UTC instants in Colombo regardless of browser timezone', () => {
    expect(formatDateTime('2026-10-22T23:15:00Z')).toBe('23/10/2026, 04:45'); expect(formatDateTime('2026-10-22T23:15:00')).toBe('23/10/2026, 04:45'); expect(formatLocalDate('2026-10-22T23:15:00Z')).toBe('23/10/2026'); expect(formatDateTime('invalid')).toBe('—');
  });
});
