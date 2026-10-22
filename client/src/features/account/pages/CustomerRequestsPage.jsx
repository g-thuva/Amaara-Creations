import { useState } from 'react';
import { Link } from 'react-router-dom';
import { requestsApi } from '@/features/account/requestsApi.js';
import { securityApi } from '@/features/account/securityApi.js';
import { customDesignApi } from '@/features/custom-design/customDesignApi.js';
import { useResource } from '@/shared/hooks/useResource.js';
import { useAuthenticatedMutation } from '@/features/auth/hooks/useAuthenticatedMutation.js';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Button } from '@/shared/components/ui/Button.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { ConfirmDialog } from '@/shared/components/ui/ConfirmDialog.jsx';

export default function CustomerRequestsPage() {
  const [page, setPage] = useState(1);
  const [cancel, setCancel] = useState(null);
  const resource = useResource(signal => requestsApi.list(page, signal), page);
  const capabilities = useResource(signal => securityApi.capabilities(signal), 'request-capabilities');
  const designs = useResource(() => customDesignApi.listDesigns({ page: 1, pageSize: 50 }), 'request-designs', Boolean(capabilities.data?.quotesEnabled));
  const { mutate, pending } = useAuthenticatedMutation();
  async function submit(event) {
    event.preventDefault(); const form = event.currentTarget;
    const data = Object.fromEntries(new FormData(form));
    const saved = await mutate(() => requestsApi.createQuote({ subject: data.subject, details: data.details, quantity: data.quantity ? Number(data.quantity) : null, customDesignId: data.customDesignId ? Number(data.customDesignId) : null }), resource.reload, 'Quote request received. Review the response here.');
    if (saved) form.reset();
  }
  return <><PageHeader title="Your requests">Review quote and privacy requests and read the latest reply. A request does not create an order or confirm a payment.</PageHeader>
    {capabilities.error && <ErrorState error={capabilities.error} retry={capabilities.reload} />}
    {capabilities.data?.quotesEnabled && <section className="s-section"><h2>Request a custom quote</h2><p>Describe your materials, quantities and artwork needs. A team member must review availability and pricing; no instant price or turnaround is promised.</p><form className="s-form" onSubmit={submit}><label>Request subject<input name="subject" required maxLength={160} /></label><label>Request details<textarea name="details" required maxLength={2000} /></label><label>Requested quantity (optional)<input name="quantity" type="number" min={1} max={100000} /></label><label>Saved design (optional)<select name="customDesignId"><option value="">No saved design</option>{designs.data?.items?.map(design => <option value={design.id} key={design.id}>{design.name}</option>)}</select></label>{designs.error && <p className="s-meta">Saved designs could not be loaded. You can send this request without attaching a design.</p>}<Button disabled={pending}>Send quote request</Button></form></section>}
    {resource.error ? <ErrorState error={resource.error} retry={resource.reload} /> : resource.loading ? <p role="status">Loading your requests…</p> : <section className="s-section"><h2>Request history</h2>{!resource.data?.items?.length ? <p>No requests yet. {capabilities.data && !capabilities.data.quotesEnabled && 'Online quote requests are not enabled.'} <Link to="/help">Read the ordering guide</Link>.</p> : resource.data.items.map(request => <article className="s-request-record" key={request.id}><h3>{request.subject}</h3><p className="s-meta">{request.kind === 'PrivacyDeletion' ? 'Deletion review' : 'Custom quote'} · {request.status} · <time dateTime={request.createdAt}>{new Date(request.createdAt).toLocaleDateString('en-LK')}</time></p><p className="s-request-text">{request.details}</p>{request.customDesignId && <p><Link to={`/custom/${request.customDesignId}`}>Open attached design</Link></p>}{request.reply && <div><h4>Latest reply</h4><p className="s-request-text">{request.reply}</p></div>}{!['Closed', 'Cancelled'].includes(request.status) && <Button variant="secondary" disabled={pending} onClick={() => setCancel(request)}>Cancel request</Button>}</article>)}</section>}
    {resource.data?.totalPages > 1 && <nav className="s-actions" aria-label="Request pages"><Button variant="secondary" disabled={page === 1 || resource.loading} onClick={() => setPage(page - 1)}>Previous requests</Button><span>Page {page} of {resource.data.totalPages}</span><Button variant="secondary" disabled={page >= resource.data.totalPages || resource.loading} onClick={() => setPage(page + 1)}>Next requests</Button></nav>}
    <ConfirmDialog open={Boolean(cancel)} title="Cancel this request?" pending={pending} onClose={() => setCancel(null)} onConfirm={async () => { if (await mutate(() => requestsApi.cancel(cancel.id), resource.reload, 'Request cancelled.')) setCancel(null); }}>The request history remains available. This action does not cancel any order.</ConfirmDialog>
  </>;
}
