import { useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { customerRequestsApi } from './customerRequestsApi.js';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAdminMutation } from '@/features/admin/shared/hooks/useAdminMutation.js';
import { ResourceState } from '@/features/admin/shared/components/ResourceState.jsx';
import { Pagination } from '@/features/admin/shared/components/Pagination.jsx';
import { PageHeader, DataTable, Callout } from '@/features/admin/shared/components/AdminUi.jsx';
import { MutationFeedback } from '@/features/admin/shared/components/MutationFeedback.jsx';
import { formatDateTime } from '@/features/admin/shared/utils/format.js';

export default function CustomerRequestsPage() {
  const { pathname } = useLocation();
  const kind = pathname.endsWith('privacy-requests') ? 'privacy' : 'quotes';
  const [page, setPage] = useState(1);
  const [selected, setSelected] = useState(null);
  const resource = useResource(signal => customerRequestsApi.list(kind, page, signal), `${kind}:${page}`);
  const mutation = useAdminMutation(resource.reload);
  async function submit(event) {
    event.preventDefault(); const data = Object.fromEntries(new FormData(event.currentTarget));
    if (await mutation.run(() => customerRequestsApi.review(selected.id, { ...data, rowVersion: selected.rowVersion }), 'Customer-visible reply saved.')) setSelected(null);
  }
  const privacy = kind === 'privacy';
  return <div className="admin-page"><PageHeader title={privacy ? 'Privacy requests' : 'Quote requests'} description={privacy ? 'Review deletion requests using approved identity and retention procedures.' : 'Review quantities, materials and attached private artwork before replying.'} />
    <Callout><p>{privacy ? 'Closing a request does not erase customer or financial records.' : 'A reply does not create an order, charge a customer or promise production.'}</p></Callout>
    <MutationFeedback {...mutation} />
    <ResourceState resource={resource} errorTitle={privacy ? "We couldn't load privacy requests" : "We couldn't load quote requests"}>
      <DataTable caption={privacy ? 'Privacy request queue' : 'Quote request queue'} rows={resource.data?.items || []} empty="No requests found." emptyDescription="New customer requests will appear here for your team to review." columns={[
        { key: 'kind', label: 'Request type', render: request => request.kind === 'PrivacyDeletion' ? 'Privacy deletion' : 'Custom quote' },
        { key: 'subject', label: 'Request', render: request => <div><strong>{request.subject}</strong><p className="s-request-text">{request.details}</p>{request.quantity && <p>Requested quantity: {request.quantity}</p>}{request.customDesignId && <Link to={`/admin/custom-designs/${request.customDesignId}`}>Open attached production design</Link>}{request.reply && <p className="s-request-text">Latest reply: {request.reply}</p>}</div> },
        { key: 'createdAt', label: 'Submitted', render: request => <time dateTime={request.createdAt} title={request.createdAt}>{formatDateTime(request.createdAt)}</time> },
        { key: 'age', label: 'Age', render: request => { const elapsed = Math.max(0, Math.floor((Date.now() - new Date(request.createdAt).getTime()) / 86400000)); return Number.isFinite(elapsed) ? elapsed === 0 ? 'Less than a day' : `${elapsed} days` : '—'; } },
        { key: 'status', label: 'State' },
        { key: 'actions', label: 'Actions', render: request => !['Closed', 'Cancelled'].includes(request.status) ? <button className="btn btn-outline-primary" disabled={mutation.pending} onClick={() => { setSelected(request); mutation.reset(); }}>Review request</button> : 'No action required' },
      ]} />
      <Pagination page={page} setPage={setPage} data={resource.data} size={20} total={resource.data?.totalCount || 0} disabled={mutation.pending} />
    </ResourceState>
    {selected && <section className="card card-body" aria-label="Review selected request"><h2>{selected.subject}</h2><form key={selected.id} className="add-product-form" onSubmit={submit}><label>Review status<select name="status" className="form-control" defaultValue={['InReview', 'Responded'].includes(selected.status) ? selected.status : 'InReview'}><option value="InReview">In review</option><option value="Responded">Responded</option><option value="Closed">Closed</option></select></label><label>Customer-visible reply<textarea name="reply" className="form-control" required maxLength={2000} defaultValue={selected.reply || ''} /></label><div className="action-buttons"><button className="btn btn-primary" disabled={mutation.pending}>Save reply</button><button className="btn btn-outline-primary" type="button" disabled={mutation.pending} onClick={() => setSelected(null)}>Cancel reply</button></div></form></section>}
  </div>;
}
