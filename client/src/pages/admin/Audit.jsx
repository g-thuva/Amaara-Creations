import { useState } from 'react';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { DataTable, ResourceState, Pager } from './OperationsUI';
export default function Audit() {
  const [page, setPage] = useState(1); const [filters, setFilters] = useState({});
  const resource = useResource(signal => api.get('audit', { ...filters, page }, signal), JSON.stringify([filters, page]));
  return <div className="admin-page"><h1>Admin audit log</h1><form className="form-row" onSubmit={e => { e.preventDefault(); setFilters(Object.fromEntries([...new FormData(e.currentTarget)].filter(([, v]) => v))); setPage(1); }}>{[['action', 'Action'], ['entityType', 'Entity type'], ['entityId', 'Entity identifier']].map(([key, label]) => <label key={key}>{label}<input className="form-control" name={key} /></label>)}<button className="btn btn-primary">Filter audit</button></form>
    <ResourceState resource={resource}><DataTable caption="Append-only audit evidence" rows={resource.data?.items || []} columns={[{ key: 'createdAt', label: 'Time', render: r => new Date(r.createdAt).toLocaleString() }, { key: 'actorUserId', label: 'Actor' }, { key: 'action', label: 'Action' }, { key: 'entityType', label: 'Entity', render: r => `${r.entityType} ${r.entityId || ''}` }, { key: 'correlationId', label: 'Correlation' }, { key: 'changes', label: 'Changes', render: r => <details><summary>View safe metadata</summary><p>Before</p><pre>{r.oldValuesJson || '—'}</pre><p>After</p><pre>{r.newValuesJson || '—'}</pre></details> }]} /><Pager page={page} setPage={setPage} total={resource.data?.totalCount || 0} size={50} /></ResourceState>
  </div>;
}
