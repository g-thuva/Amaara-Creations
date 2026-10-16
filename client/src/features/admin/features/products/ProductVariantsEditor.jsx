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
      <table>
        <caption>Product variants</caption>
        <thead>
          <tr>
            <th scope="col">SKU</th>
            <th scope="col">Name</th>
            <th scope="col">Stock</th>
            <th scope="col">State</th>
          </tr>
        </thead>
        <tbody>
          {variants.map((variant) => (
            <tr key={variant.id}>
              <td>{variant.sku}</td>
              <td>{variant.name}</td>
              <td>{variant.stockQuantity}</td>
              <td>{variant.isActive ? "Active" : "Archived"}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  );
}
