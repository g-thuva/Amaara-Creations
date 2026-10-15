import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { checkoutMoney } from '../../services/checkoutApi';
import { DataTable, ResourceState, Pager } from './OperationsUI';
export default function AdOrders() {
  const [page, setPage] = useState(1); const [search, setSearch] = useState(''); const [status, setStatus] = useState('');
  const resource = useResource(signal => api.get('orders', { pageNumber: page, search, status: status || undefined }, signal), JSON.stringify([page, search, status]));
  return <div className="admin-page"><h1>Order management</h1><div className="form-row"><label>Search order or customer<input className="form-control" value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} /></label><label>Order state<select className="form-control" value={status} onChange={e => { setStatus(e.target.value); setPage(1); }}><option value="">All</option>{['Pending', 'PendingPayment', 'Confirmed', 'Processing', 'Shipped', 'Delivered', 'Completed', 'Cancelled', 'PaymentReview'].map(s => <option key={s}>{s}</option>)}</select></label></div><ResourceState resource={resource}><DataTable caption="Customer orders" rows={resource.data?.orders || []} columns={[{ key: 'orderNumber', label: 'Order', render: r => <Link to={`/admin/orders/${r.id}`}>{r.orderNumber}</Link> }, { key: 'userName', label: 'Customer' }, { key: 'createdAt', label: 'Created', render: r => new Date(r.createdAt).toLocaleString() }, { key: 'total', label: 'Total', render: r => checkoutMoney(r.total, r.currency) }, { key: 'status', label: 'Order state' }, { key: 'paymentStatus', label: 'Payment state' }, { key: 'fulfilmentStatus', label: 'Fulfilment' }]} /><Pager page={page} setPage={setPage} total={resource.data?.totalCount || 0} size={20} /></ResourceState></div>;
}
