import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { DataTable, ResourceState, Pager } from './OperationsUI';
import { checkoutMoney } from '../../services/checkoutApi';
export default function CustomDesigns() {
  const [filters, setFilters] = useState({ search: '', designStatus: 'Ordered', proofStatus: '', productionStatus: '', page: 1, pageSize: 20 });
  const resource = useResource(signal => api.get('custom-designs', filters, signal), JSON.stringify(filters));
  return <div className="admin-page"><h1>Custom design production</h1><p>Ordered custom stickers, proofs and production status.</p><div className="card card-body form-row"><label>Search order or customer<input className="form-control" value={filters.search} onChange={e => setFilters({ ...filters, search: e.target.value, page: 1 })} /></label>{[['designStatus', 'Design state', ['Ordered', 'Archived']], ['proofStatus', 'Proof state', ['Preparing', 'AwaitingApproval', 'ChangesRequested', 'Approved', 'NotRequired']], ['productionStatus', 'Production state', ['NotStarted', 'Queued', 'Printing', 'Finishing', 'QualityCheck', 'Ready']]].map(([key, label, choices]) => <label key={key}>{label}<select className="form-control" value={filters[key]} onChange={e => setFilters({ ...filters, [key]: e.target.value, page: 1 })}><option value="">All</option>{choices.map(v => <option key={v}>{v}</option>)}</select></label>)}</div>
    <ResourceState resource={resource}><DataTable caption="Ordered production jobs" rows={resource.data?.items || []} columns={[{ key: 'name', label: 'Design', render: r => <Link to={`/admin/custom-designs/${r.id}`}>{r.name}</Link> }, { key: 'orderNumber', label: 'Order' }, { key: 'customerName', label: 'Customer' }, { key: 'spec', label: 'Specification', render: r => `${r.width} × ${r.height} cm · ${r.quantity}` }, { key: 'proofStatus', label: 'Proof' }, { key: 'productionStatus', label: 'Production' }, { key: 'calculatedPrice', label: 'Subtotal', render: r => checkoutMoney(r.calculatedPrice, r.currency) }]} /><Pager page={filters.page} setPage={page => setFilters({ ...filters, page })} total={resource.data?.totalCount || 0} size={20} /></ResourceState>
  </div>;
}
