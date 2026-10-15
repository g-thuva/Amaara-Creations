import { useState } from 'react';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { ConfirmDialog } from '../../components/storefront/UI';
import { DataTable, ResourceState, Pager, useMutation } from './OperationsUI';

export default function Reviews() {
  const [page, setPage] = useState(1); const [status, setStatus] = useState(''); const [intent, setIntent] = useState(null); const [reason, setReason] = useState('');
  const resource = useResource(signal => api.get('reviews', { status: status || undefined, pageNumber: page }, signal), JSON.stringify([page, status]));
  const mutation = useMutation(resource.reload);
  return <div className="admin-page"><h1>Review moderation</h1><p>Verified purchase comes from eligible fulfilled order history. New and edited reviews require moderation before publication.</p>
    <label>Moderation state<select className="form-control" value={status} onChange={e => { setStatus(e.target.value); setPage(1); }}><option value="">All</option>{['Pending', 'Published', 'Hidden', 'Deleted'].map(s => <option key={s}>{s}</option>)}</select></label>{mutation.feedback}
    <ResourceState resource={resource}><DataTable caption="Customer reviews" rows={resource.data?.reviews || []} columns={[{ key: 'productName', label: 'Product' }, { key: 'userName', label: 'Customer' }, { key: 'rating', label: 'Rating' }, { key: 'comment', label: 'Review' }, { key: 'verifiedPurchase', label: 'Purchase', render: r => r.verifiedPurchase ? 'Verified purchase' : 'Unverified' }, { key: 'moderationStatus', label: 'State' }, { key: 'moderationReason', label: 'Last reason' }, { key: 'actions', label: 'Moderate', render: r => <form onSubmit={e => { e.preventDefault(); const f = new FormData(e.currentTarget); setIntent({ id: r.id, rowVersion: r.rowVersion, status: f.get('status') }); setReason(f.get('reason')); }}><label>New state<select className="form-control" name="status">{['Published', 'Hidden', 'Deleted'].map(s => <option key={s}>{s}</option>)}</select></label><label>Reason<textarea className="form-control" name="reason" required maxLength="500" /></label><button className="btn btn-outline-primary" disabled={mutation.pending}>Review moderation for {r.id}</button></form> }]} /><Pager page={page} setPage={setPage} total={resource.data?.totalCount || 0} size={20} /></ResourceState>
    <ConfirmDialog open={Boolean(intent)} onClose={() => setIntent(null)} title="Confirm review moderation" pending={mutation.pending} onConfirm={async () => { await mutation.run(() => api.put(`reviews/${intent.id}/moderation`, { ...intent, reason })); setIntent(null); }}>{intent?.status}: {reason}</ConfirmDialog>
  </div>;
}
