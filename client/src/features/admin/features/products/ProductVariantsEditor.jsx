import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { StatusChip } from '@/features/admin/shared/components/StatusChip.jsx';

export function ProductVariantsEditor({ variants, variantForm, setVariantForm, saveVariant, pending }) {
  return <>
    <h3 className="pf-section-title">Product variants</h3>
    <form onSubmit={saveVariant}>
      <fieldset className="form-row admin-form-fields" disabled={pending}>
        <label className="form-group">Variant SKU <span aria-hidden="true" className="admin-required" />
          <input className="form-control" aria-label="Variant SKU" required maxLength={64}
            value={variantForm.sku} onChange={event => setVariantForm({ ...variantForm, sku: event.target.value })} />
        </label>
        <label className="form-group">Variant name <span aria-hidden="true" className="admin-required" />
          <input className="form-control" aria-label="Variant name" required maxLength={120}
            value={variantForm.name} onChange={event => setVariantForm({ ...variantForm, name: event.target.value })} />
        </label>
        <label className="form-group">Price override
          <input className="form-control" type="number" min="0" step="0.01" aria-label="Price override"
            value={variantForm.priceOverride} onChange={event => setVariantForm({ ...variantForm, priceOverride: event.target.value })} />
        </label>
        <label className="form-group">Variant stock
          <input className="form-control" type="number" min="0" aria-label="Variant stock"
            value={variantForm.stockQuantity} onChange={event => setVariantForm({ ...variantForm, stockQuantity: event.target.value })} />
        </label>
        <button className="btn btn-primary" type="submit" disabled={pending}>Add Variant</button>
      </fieldset>
    </form>
    <DataTable caption="Product variants" rows={variants} empty="No variants yet" emptyDescription="Add a variant above when this product needs another size or option." columns={[
      { key: 'sku', label: 'SKU' }, { key: 'name', label: 'Name' }, { key: 'stockQuantity', label: 'Stock' },
      { key: 'state', label: 'State', render: variant => <StatusChip status={variant.isActive ? 'Active' : 'Archived'} /> },
    ]} />
  </>;
}
