import { EmptyState } from './EmptyState.jsx';
import { ErrorState } from './ErrorState.jsx';
import { StatusChip } from './StatusChip.jsx';
const stateKeys = new Set(['status', 'state', 'paymentStatus', 'productionStatus', 'proofStatus', 'fulfilmentStatus', 'moderationStatus']);
export function DataTable({ caption, columns, rows = [], rowKey = (row, index) => row.id ?? index, empty = 'No records found.', emptyDescription = 'Records will appear here when they are available.', emptyAction, loading = false, error, retry, zebra = false, sort, onSort, captionVisible = true }) {
  const cell = (column, row) => {
    if (column.render) return column.render(row);
    const value = row[column.key];
    if (stateKeys.has(column.key) && value != null) return <StatusChip status={value} />;
    if (column.truncate && value != null) return <span className="admin-cell-truncate" title={String(value)}>{String(value)}</span>;
    return String(value ?? '—');
  };
  return <div className="admin-table-shell"><div className="table-responsive" tabIndex={0} role="region" aria-label={caption}>
    <table className={`admin-table ${zebra ? 'admin-table-zebra' : ''}`} aria-busy={loading || undefined}><caption className={captionVisible ? undefined : 'admin-sr-only'}>{caption}</caption><thead><tr>{columns.map(column => <th scope="col" key={column.key} aria-sort={sort?.key === column.key ? sort.direction === 'asc' ? 'ascending' : 'descending' : undefined}>{column.sortable && onSort ? <button type="button" className="admin-sort" onClick={() => onSort(column.key)}>{column.label}<span aria-hidden="true">↕</span></button> : column.label}</th>)}</tr></thead>
      <tbody>{error ? <tr><td colSpan={columns.length}><ErrorState message={typeof error === 'string' ? error : 'The records could not be loaded.'} retry={retry} /></td></tr> : loading && !rows.length ? Array.from({ length: 5 }, (_, index) => <tr key={index} aria-hidden="true">{columns.map(column => <td key={column.key}><span className="admin-skeleton" /></td>)}</tr>) : rows.length ? rows.map((row, index) => <tr key={rowKey(row, index)}>{columns.map(column => <td key={column.key} className={['amount', 'total', 'price', 'subtotal', 'settled', 'refunded', 'calculatedPrice', 'discount'].includes(column.key) ? 'admin-cell-money' : /At$|Date$/.test(column.key) ? 'admin-cell-date' : undefined}>{cell(column, row)}</td>)}</tr>) : <tr><td colSpan={columns.length}><EmptyState title={empty} description={emptyDescription} action={emptyAction} /></td></tr>}</tbody>
    </table>{loading && !rows.length && <span className="admin-sr-only" role="status">Loading {caption}</span>}
  </div></div>;
}
