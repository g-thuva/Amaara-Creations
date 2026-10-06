import { Link } from 'react-router-dom';
import { Button, EmptyState, ErrorState, Icon, PageHeader, Price, Skeleton } from '../components/storefront/UI';
import { useResource } from '../hooks/useResource';
import { useStore } from '../contexts/StoreContext';
import { customBuilderApi } from '../services/customBuilderApi';
import { date } from '../utils/storefront';

export default function SavedDesigns() {
  const designs = useResource(() => customBuilderApi.listDesigns({ pageSize: 50 }), 'saved-designs');
  const { cart, mutate } = useStore();
  const act = (action, message) => mutate(action, designs.reload, message);
  const add = (id) => mutate(async () => { await customBuilderApi.addToCart(id); await cart.reload(); }, designs.reload, 'Custom design added to your cart.');

  return (
    <>
      <PageHeader eyebrow="Reusable designs" title="Saved designs">Reopen, duplicate or order a custom sticker design.</PageHeader>
      {designs.loading ? <Skeleton count={3} /> : designs.error ? <ErrorState error={designs.error} retry={designs.reload} /> : !designs.data?.items?.length ? (
        <EmptyState title="No saved designs yet" to="/custom" label="Build a custom sticker" />
      ) : <div className="s-design-grid">{designs.data.items.map((design) => (
        <article className="s-panel s-design-card" key={design.id}>
          <div className="s-sticker-mini" aria-hidden="true">{design.customText || 'Custom'}</div>
          <div><span className="s-badge">{design.status}</span><h2>{design.name}</h2><p>{design.width} × {design.height} {design.measurementUnit || 'cm'} · {design.quantity} pieces</p><p>Updated {date(design.updatedAt)}</p>{design.subtotal != null && <Price value={design.subtotal} />}</div>
          <div className="s-actions">
            <Link className="s-button s-button-secondary" to={`/custom/${design.id}`}>Edit</Link>
            {design.proofStatus && !['Preparing', 'NotRequired'].includes(design.proofStatus) && <Link className="s-button s-button-secondary" to={`/account/designs/${design.id}/proof`}>Proof</Link>}
            <Button onClick={() => add(design.id)} disabled={design.status === 'Ordered'}>Add to cart</Button>
            <Button variant="text" onClick={() => act(() => customBuilderApi.duplicateDesign(design.id), 'Design duplicated.')}>Duplicate</Button>
            <Button variant="text" disabled={['Ordered', 'InCart'].includes(design.status)} onClick={() => act(() => customBuilderApi.archiveDesign(design.id), 'Design archived.')}>Archive</Button>
          </div>
        </article>
      ))}</div>}
      <Link className="s-text-link" to="/custom">Create another design <Icon name="arrow" /></Link>
    </>
  );
}
