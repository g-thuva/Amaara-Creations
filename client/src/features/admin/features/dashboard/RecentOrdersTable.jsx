import { useState } from 'react';
import { Link } from 'react-router-dom';
import AppIcon from '@/shared/components/ui/Icon.jsx';
import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { formatDateTime } from '@/features/admin/shared/utils/format.js';
import { utcInstant } from '@/utils/dateBuckets.js';
import { formatMoney } from '@/utils/formatMoney.js';

function relativeDate(value) {
  const date = utcInstant(value);
  if (!date) return '—';
  const seconds = (date.getTime() - Date.now()) / 1000;
  if (Math.abs(seconds) >= 7 * 86400) return formatDateTime(value).split(',')[0];
  const [unit, divisor] = Math.abs(seconds) >= 86400 ? ['day', 86400] : Math.abs(seconds) >= 3600 ? ['hour', 3600] : ['minute', 60];
  return new Intl.RelativeTimeFormat('en-GB', { numeric: 'auto', style: 'short' }).format(Math.round(seconds / divisor), unit);
}
function OrderNumber({ order, canViewOrders }) {
  const [message, setMessage] = useState('');
  const number = order.orderNumber || '#' + order.id;
  const copy = async () => {
    try { await navigator.clipboard.writeText(number); setMessage('Order number copied'); }
    catch { setMessage('Copy unavailable. The full order number is available in the order link.'); }
  };
  return <span className="sales-order-id">{canViewOrders ? <Link title={number} to={`/admin/orders/${order.id}`}>{number}</Link> : <code title={number}>{number}</code>}<button type="button" aria-label={`Copy order ${number}`} onClick={copy}><AppIcon name="layers" /></button><span role="status" className="admin-sr-only">{message}</span></span>;
}
export function RecentOrdersTable({ resource, canViewOrders = true }) {
  const rows = resource.data || [];
  const lkr = rows.filter(order => order.currency === 'LKR');
  return <section className="sales-panel sales-recent"><div className="sales-panel-header"><div><h2>Recent orders</h2><p>Latest LKR orders · all environments</p></div>{canViewOrders && <Link to="/admin/orders">View all <span aria-hidden="true">↗</span></Link>}</div>
    <div className="sales-scroll"><DataTable caption="Recent order activity" captionVisible={false} rows={lkr.slice(0, 5)} loading={resource.loading} error={resource.error && 'Recent orders could not be loaded.'} retry={resource.reload} empty="No orders yet" emptyDescription="New orders will appear here." columns={[
      { key: 'orderNumber', label: 'Order #', render: order => <OrderNumber order={order} canViewOrders={canViewOrders} /> },
      { key: 'customer', label: 'Customer', render: order => { const name = order.customerName || order.customerEmail || '—'; return <span className="sales-customer"><span aria-hidden="true" className="sales-customer-avatar">{name.split(/\s+/).map(part => part[0]).slice(0, 2).join('').toUpperCase()}</span><span className="sales-customer-name" title={name}>{name}</span></span>; } },
      { key: 'orderDate', label: 'Date', render: order => <time dateTime={utcInstant(order.orderDate)?.toISOString()} title={formatDateTime(order.orderDate)}>{relativeDate(order.orderDate)}</time> },
      { key: 'total', label: 'Amount', render: order => formatMoney(order.total) },
      { key: 'status', label: 'Status' },
    ]} /></div>
    {rows.length > lkr.length && <p className="sales-data-note">{rows.length - lkr.length} recent orders in other currencies are omitted.</p>}
  </section>;
}
