import { Link } from 'react-router-dom';
import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { formatDate, formatIsoCurrency as money } from '@/features/admin/shared/utils/format.js';
export function RecentOrdersTable({ recentOrders, canViewOrders = true }) {
  return <section className="dashboard-card"><div className="card-header"><h2>Recent Orders</h2>{canViewOrders && <Link to="/admin/orders" className="view-all">View All</Link>}</div><DataTable caption="Recent order activity" captionVisible={false} rows={recentOrders} empty="No orders yet" emptyDescription="New orders will appear here." columns={[
    { key: 'orderNumber', label: 'Order ID', render: order => canViewOrders ? <Link className="admin-cell-truncate" title={order.orderNumber} to={`/admin/orders/${order.id}`}>{order.orderNumber || '#' + order.id}</Link> : <span className="admin-cell-truncate" title={order.orderNumber}>{order.orderNumber || '#' + order.id}</span> },
    { key: 'customer', label: 'Customer', render: order => order.customerName || order.customerEmail },
    { key: 'orderDate', label: 'Date', render: order => formatDate(order.orderDate) },
    { key: 'total', label: 'Amount', render: order => money(order.total, order.currency || 'LKR') },
    { key: 'status', label: 'Status' },
  ]} /></section>;
}
