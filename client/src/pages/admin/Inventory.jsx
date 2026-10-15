import { useState } from 'react';
import { useResource } from '../../hooks/useResource';
import { operationsApi as api } from '../../services/operationsApi';
import { ConfirmDialog } from '../../components/storefront/UI';
import { DataTable, ResourceState, Pager, useMutation } from './OperationsUI';

export default function Inventory() {
  const [page, setPage] = useState(1); const [search, setSearch] = useState(''); const [selected, setSelected] = useState(null); const [intent, setIntent] = useState(null);
  const resource = useResource(signal => api.get('inventory', { page, search }, signal), JSON.stringify([page, search]));
  const movements = useResource(signal => api.get('inventory/movements', { productId: selected?.productId }, signal), String(selected?.productId));
  const mutation = useMutation(() => { resource.reload(); movements.reload(); });
  const submit = event => {
    event.preventDefault(); const form = Object.fromEntries(new FormData(event.currentTarget));
    setIntent({ variantId: selected.variantId, expectedAvailable: selected.available, quantityDelta: Number(form.delta), reason: form.reason, lowStockThreshold: Number(form.threshold) });
  };
  return <div className="admin-page"><h1>Inventory ledger</h1><p>On hand includes reserved goods. Available stock is protected by checkout reservations.</p>
    <label>Search product or SKU<input className="form-control" value={search} onChange={e => { setSearch(e.target.value); setPage(1); }} /></label>
    {mutation.feedback}<ResourceState resource={resource}><DataTable caption="Stock balances" rows={resource.data?.items || []} rowKey={r => `${r.productId}-${r.variantId}`} columns={[
      { key: 'product', label: 'Product' }, { key: 'variant', label: 'Variant' }, { key: 'sku', label: 'SKU' }, { key: 'onHand', label: 'On hand' }, { key: 'reserved', label: 'Reserved' }, { key: 'available', label: 'Available', render: r => <>{r.available}{r.available <= r.lowStockThreshold && ' · Low stock'}</> }, { key: 'lowStockThreshold', label: 'Low-stock threshold' },
      { key: 'action', label: 'Action', render: r => <button className="btn btn-outline-primary" onClick={() => setSelected(r)}>Adjust {r.sku || r.product}</button> },
    ]} /><Pager page={page} setPage={setPage} total={resource.data?.totalCount || 0} size={20} /></ResourceState>
    {selected && <section className="card"><h2>Adjust {selected.product} · {selected.variant}</h2><p>Available when selected: {selected.available}</p><form className="card-body add-product-form" key={`${selected.productId}-${selected.variantId}-${selected.available}`} onSubmit={submit}>
      <label>Quantity change<input className="form-control" name="delta" type="number" step="1" required /></label><label>Low-stock threshold<input className="form-control" name="threshold" type="number" min="0" defaultValue={selected.lowStockThreshold} required /></label><label>Reason<textarea className="form-control" name="reason" maxLength="300" required /></label><button className="btn btn-primary" disabled={mutation.pending}>Review adjustment</button><button type="button" className="btn btn-outline-primary" onClick={() => setSelected(null)}>Close adjustment</button>
    </form></section>}
    <h2>Inventory movements{selected ? ` · ${selected.product}` : ''}</h2><ResourceState resource={movements}><DataTable caption="Available-stock movement history" rows={movements.data?.items || []} columns={[{ key: 'createdAt', label: 'Time', render: r => new Date(r.createdAt).toLocaleString() }, { key: 'movementType', label: 'Type' }, { key: 'quantityDelta', label: 'Delta' }, { key: 'newQuantity', label: 'Available after' }, { key: 'orderId', label: 'Order' }, { key: 'reason', label: 'Reason' }, { key: 'performedByUserId', label: 'Actor' }]} /></ResourceState>
    <ConfirmDialog open={Boolean(intent)} onClose={() => setIntent(null)} title="Confirm inventory adjustment" pending={mutation.pending} onConfirm={async () => { const ok = await mutation.run(() => api.post(`inventory/${selected.productId}/adjustments`, intent), 'Inventory adjustment recorded.'); setIntent(null); if (ok) setSelected(null); }}>Change available quantity by {intent?.quantityDelta}. Reason: {intent?.reason}</ConfirmDialog>
  </div>;
}
