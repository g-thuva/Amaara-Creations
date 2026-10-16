import { Link } from 'react-router-dom';
import { Button } from '@/shared/components/ui/Button.jsx';
import { Price } from '@/shared/components/ui/Price.jsx';
import { formatDate as date } from '@/shared/utils/formatDate.ts';
export default function SavedDesignCard({ design, pending, duplicate, archive, add }) {
  return (
        <article className="s-panel s-design-card" key={design.id}>
          <div className="s-sticker-mini" aria-hidden="true">{design.customText || 'Custom'}</div>
          <div><span className="s-badge">{design.status}</span><h2>{design.name}</h2><p>{design.width} × {design.height} {design.measurementUnit || 'cm'} · {design.quantity} pieces</p><p>Updated {date(design.updatedAt)}</p>{design.subtotal != null && <Price value={design.subtotal} currency={design.currency} />}</div>
          <div className="s-actions">
            <Link className="s-button s-button-secondary" to={`/custom/${design.id}`}>Edit</Link>
            {design.proofStatus && !['Preparing', 'NotRequired'].includes(design.proofStatus) && <Link className="s-button s-button-secondary" to={`/account/designs/${design.id}/proof`}>Proof</Link>}
            <Button onClick={() => add(design.id)} disabled={pending || design.status === 'Ordered'}>Add to cart</Button>
            <Button variant="text" disabled={pending} onClick={() => duplicate(design.id)}>Duplicate</Button>
            <Button variant="text" disabled={pending || ['Ordered', 'InCart'].includes(design.status)} onClick={() => archive(design.id)}>Archive</Button>
          </div>
        </article>
  );
}
