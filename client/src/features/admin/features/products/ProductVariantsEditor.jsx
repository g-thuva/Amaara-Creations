import { DataTable } from '@/features/admin/shared/components/DataTable.jsx';
import { StatusChip } from '@/features/admin/shared/components/StatusChip.jsx';
export function ProductVariantsEditor({
  variants,
  variantForm,
  setVariantForm,
  saveVariant,
  pending,
}) {
  return (
    <>
      <form onSubmit={saveVariant} className="form-row">
        <input
          className="form-control"
          aria-label="Variant SKU"
          value={variantForm.sku}
          required
          placeholder="Variant SKU"
          onChange={(e) =>
            setVariantForm({ ...variantForm, sku: e.target.value })
          }
        />
        <input
          className="form-control"
          aria-label="Variant name"
          value={variantForm.name}
          required
          placeholder="Variant name"
          onChange={(e) =>
            setVariantForm({ ...variantForm, name: e.target.value })
          }
        />
        <input
          className="form-control"
          type="number"
          min="0"
          aria-label="Price override"
          value={variantForm.priceOverride}
          placeholder="Price override"
          onChange={(e) =>
            setVariantForm({ ...variantForm, priceOverride: e.target.value })
          }
        />
        <input
          className="form-control"
          type="number"
          min="0"
          aria-label="Variant stock"
          value={variantForm.stockQuantity}
          onChange={(e) =>
            setVariantForm({ ...variantForm, stockQuantity: e.target.value })
          }
        />
        <button className="btn btn-primary" type="submit" disabled={pending}>
          Add Variant
        </button>
      </form>
      <DataTable caption="Product variants" rows={variants} empty="No variants yet" emptyDescription="Add a variant above when this product needs another size or option." columns={[{ key: 'sku', label: 'SKU' }, { key: 'name', label: 'Name' }, { key: 'stockQuantity', label: 'Stock' }, { key: 'state', label: 'State', render: variant => <StatusChip status={variant.isActive ? 'Active' : 'Archived'} /> }]} />
    </>
  );
}
