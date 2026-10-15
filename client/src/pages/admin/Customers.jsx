import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { DataTable, ResourceState, Pager } from './OperationsUI';
export default function Customers() {
  const [page, setPage] = useState(1); const [search, setSearch] = useState('');
  const resource = useResource(signal => api.get('customers', { pageNumber: page, search }, signal), JSON.stringify([page, search]));
  return <div className="admin-page"><h1>Customers</h1><label>Search name or email<input className="form-control" value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} /></label><ResourceState resource={resource}><DataTable caption="Registered customers" rows={resource.data?.customers || []} columns={[{ key: 'name', label: 'Customer', render: r => <Link to={`/admin/customers/${r.id}`}>{r.name}</Link> }, { key: 'email', label: 'Email' }, { key: 'phone', label: 'Phone' }, { key: 'createdAt', label: 'Joined', render: r => new Date(r.createdAt).toLocaleDateString() }]} /><Pager page={page} setPage={setPage} total={resource.data?.totalCount || 0} size={20} /></ResourceState></div>;
}
