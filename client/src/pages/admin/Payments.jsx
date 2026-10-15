import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { checkoutMoney } from '../../services/checkoutApi';
import { DataTable, ResourceState, Pager } from './OperationsUI';
export default function Payments() {
  const [page, setPage] = useState(1); const [status, setStatus] = useState(''); const [mode, setMode] = useState('');
  const resource = useResource(signal => api.get('payments', { page, status: status || undefined, mode: mode || undefined }, signal), JSON.stringify([page, status, mode]));
  return <div className="admin-page"><h1>Payments and refunds</h1><div className="form-row"><label>Payment state<select className="form-control" value={status} onChange={e => { setStatus(e.target.value); setPage(1); }}><option value="">All</option>{['Pending', 'Authorized', 'Paid', 'Failed', 'Cancelled', 'PartiallyRefunded', 'Refunded'].map(s => <option key={s}>{s}</option>)}</select></label><label>Environment<select className="form-control" value={mode} onChange={e => { setMode(e.target.value); setPage(1); }}><option value="">All (including historical)</option><option>TEST</option><option>LIVE</option></select></label></div>
    <ResourceState resource={resource}><DataTable caption="Payment attempts" rows={resource.data?.items || []} columns={[{ key: 'orderNumber', label: 'Order and operations', render: r => <Link to={`/admin/orders/${r.orderId}`}>{r.orderNumber}</Link> }, { key: 'provider', label: 'Provider' }, { key: 'providerMode', label: 'Mode' }, { key: 'status', label: 'State' }, { key: 'amount', label: 'Amount', render: r => checkoutMoney(r.amount, r.currency) }, { key: 'createdAt', label: 'Created', render: r => new Date(r.createdAt).toLocaleString() }, { key: 'paidAt', label: 'Confirmed', render: r => r.paidAt ? new Date(r.paidAt).toLocaleString() : 'Unconfirmed' }, { key: 'providerGatewayId', label: 'Gateway' }, { key: 'providerReference', label: 'Reference' }]} /><Pager page={page} setPage={setPage} size={30} total={resource.data?.totalCount || 0} /></ResourceState>
  </div>;
}
