import { useState } from 'react';
import { Link, useParams } from 'react-router-dom';
import { Button, EmptyState, ErrorState, PageHeader, Skeleton } from '../components/storefront/UI';
import { useResource } from '../hooks/useResource';
import { useStore } from '../contexts/StoreContext';
import { customBuilderApi } from '../services/customBuilderApi';
import { date } from '../utils/storefront';

export default function ProofReview() {
  const { id } = useParams();
  const design = useResource(() => customBuilderApi.getDesign(id), id);
  const { mutate } = useStore();
  const [comment, setComment] = useState('');
  const latest = design.data?.proofRevisions?.[0];

  const download = async (proof) => {
    const blob = await customBuilderApi.downloadPrivate(proof.downloadUrl);
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a'); anchor.href = url; anchor.download = `proof-${proof.revisionNumber}`; anchor.click();
    window.setTimeout(() => URL.revokeObjectURL(url), 1000);
  };
  const respond = async (approve) => {
    if (!latest) return;
    await mutate(() => approve ? customBuilderApi.approveProof(id, latest.revisionNumber, comment) : customBuilderApi.requestProofChanges(id, latest.revisionNumber, comment), design.reload, approve ? 'Proof approved.' : 'Change request sent.');
    setComment('');
  };

  if (design.loading) return <Skeleton count={2} />;
  if (design.error) return <ErrorState error={design.error} retry={design.reload} />;
  return <>
    <Link to="/account/designs" className="s-text-link">← Saved designs</Link>
    <PageHeader eyebrow="Production proof" title={design.data.name}>Review the latest proof revision before production continues.</PageHeader>
    {!latest ? <EmptyState title="No proof is available yet" /> : <div className="s-panel s-proof-review">
      <div className="s-row"><h2>Revision {latest.revisionNumber}</h2><span className="s-badge">{latest.status}</span></div>
      <p>Uploaded {date(latest.createdAt)}</p>{latest.customerVisibleMessage && <p>{latest.customerVisibleMessage}</p>}
      <Button variant="secondary" onClick={() => download(latest)}>Download proof</Button>
      {latest.status === 'AwaitingApproval' && <div className="s-form"><label>Response note<textarea rows="4" maxLength="1000" value={comment} onChange={(event) => setComment(event.target.value)} /></label><div className="s-actions"><Button onClick={() => respond(true)}>Approve revision {latest.revisionNumber}</Button><Button variant="secondary" disabled={!comment.trim()} onClick={() => respond(false)}>Request changes</Button></div></div>}
      {latest.customerResponse && <p><strong>Your response:</strong> {latest.customerResponse}</p>}
      <details><summary>Proof history</summary><ol>{design.data.proofRevisions.map((proof) => <li key={proof.id}>Revision {proof.revisionNumber}: {proof.status}</li>)}</ol></details>
    </div>}
  </>;
}
