import { useState } from 'react';
import { useLocation } from 'react-router-dom';
import { customerRequestsApi } from './customerRequestsApi.js';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAdminMutation } from '@/features/admin/shared/hooks/useAdminMutation.js';
import { ResourceState } from '@/features/admin/shared/components/ResourceState.jsx';
import { Pagination } from '@/features/admin/shared/components/Pagination.jsx';

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
  return <div className="admin-page"><h1>{kind === 'privacy' ? 'Privacy requests' : 'Quote requests'}</h1><p>{kind === 'privacy' ? 'Review deletion requests against approved retention and identity procedures. Closing a request does not erase customer or financial records.' : 'Review actual quantities, materials and attached private artwork. A reply does not create an order, charge a customer or promise production.'}</p>{mutation.error && <p role="alert">{mutation.error}</p>}{mutation.success && <p role="status">{mutation.success}</p>}<ResourceState resource={resource}>{!resource.data?.items?.length ? <p>No requests found.</p> : resource.data.items.map(request => <article className="admin-request-record" key={request.id}><h2>{request.subject}</h2><p>{request.status} · {new Date(request.createdAt).toLocaleString('en-LK')}</p><p className="s-request-text">{request.details}</p>{request.quantity && <p>Requested quantity: {request.quantity}</p>}{request.customDesignId && <p><a href={`${import.meta.env.BASE_URL}admin/custom-designs/${request.customDesignId}`}>Open attached production design</a></p>}{request.reply && <p className="s-request-text">Latest reply: {request.reply}</p>}{!['Closed', 'Cancelled'].includes(request.status) && <button className="btn btn-outline-primary" disabled={mutation.pending} onClick={() => { setSelected(request); mutation.reset(); }}>Review request</button>}{selected?.id === request.id && <form onSubmit={submit}><label>Review status<select name="status" className="form-control" defaultValue={['InReview', 'Responded'].includes(request.status) ? request.status : 'InReview'}><option value="InReview">In review</option><option value="Responded">Responded</option><option value="Closed">Closed</option></select></label><label>Customer-visible reply<textarea name="reply" className="form-control" required maxLength={2000} defaultValue={request.reply || ''} /></label><div className="action-buttons"><button className="btn btn-primary" disabled={mutation.pending}>Save reply</button><button className="btn btn-outline-primary" type="button" disabled={mutation.pending} onClick={() => setSelected(null)}>Cancel reply</button></div></form>}</article>)}<Pagination page={page} setPage={setPage} data={resource.data} size={20} total={resource.data?.totalCount || 0} disabled={mutation.pending} /></ResourceState></div>;
}
