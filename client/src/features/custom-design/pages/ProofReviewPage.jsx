import { useProofReview } from '@/features/custom-design/hooks/useProofReview.js';
import { Link, useParams } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import { EmptyState } from '@/shared/components/feedback/EmptyState.jsx';
import { ErrorState } from '@/shared/components/feedback/ErrorState.jsx';
import { PageHeader } from '@/shared/components/ui/PageHeader.jsx';
import { Skeleton } from '@/shared/components/feedback/Skeleton.jsx';
import { formatDate as date } from '@/shared/utils/formatDate.ts';

export default function ProofReviewPage() {
  const { id } = useParams();
  const { design, latest, pending, comment, setComment, download, respond } = useProofReview(id);
  if (design.loading) return <Skeleton count={2} />;
  if (design.error) return <ErrorState error={design.error} retry={design.reload} />;
  return <>
    <Link to="/account/designs" className="s-text-link">← Saved designs</Link>
    <PageHeader eyebrow="Production proof" title={design.data.name}>Review the latest proof revision before production continues.</PageHeader>
    {!latest ? <EmptyState to="/products" label="Explore products" title="No proof is available yet" /> : <div className="s-panel s-proof-review">
      <div className="s-row"><h2>Revision {latest.revisionNumber}</h2><span className="s-badge">{latest.status}</span></div>
      <p>Uploaded {date(latest.createdAt)}</p>{latest.customerVisibleMessage && <p>{latest.customerVisibleMessage}</p>}
      <Button variant="secondary" disabled={pending} onClick={() => download(latest)}>Download proof</Button>
      {latest.status === 'AwaitingApproval' && <div className="s-form"><label>Response note<textarea rows="4" maxLength="1000" value={comment} onChange={(event) => setComment(event.target.value)} /></label><div className="s-actions"><Button disabled={pending} onClick={() => respond(true)}>Approve revision {latest.revisionNumber}</Button><Button variant="secondary" disabled={pending || !comment.trim()} onClick={() => respond(false)}>Request changes</Button></div></div>}
      {latest.customerResponse && <p><strong>Your response:</strong> {latest.customerResponse}</p>}
      <details><summary>Proof history</summary><ol>{design.data.proofRevisions.map((proof) => <li key={proof.id}>Revision {proof.revisionNumber}: {proof.status}</li>)}</ol></details>
    </div>}
  </>;
}
