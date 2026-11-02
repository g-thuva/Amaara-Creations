export function CollectionForm({
  products,
  form,
  setForm,
  selectedProducts,
  setSelectedProducts,
  save,
  pending,
  formId,
}) {
  return (
    <form id={formId} className="add-product-form" onSubmit={save}>
      <fieldset className="admin-form-fields" disabled={pending}>

        <div className="form-row">
          <label className="form-group">
            Name <span aria-hidden="true" className="admin-required" />
            <input
              className="form-control"
              required
              data-dialog-autofocus
              value={form.name}
              onChange={(e) => setForm({ ...form, name: e.target.value })}
            />
          </label>
          <label className="form-group">
            Slug
            <input
              className="form-control"
              value={form.slug || ""}
              onChange={(e) => setForm({ ...form, slug: e.target.value })}
            />
          </label>
        </div>
        <label className="form-group">
          Description
          <textarea
            className="form-control"
            value={form.description || ""}
            onChange={(e) => setForm({ ...form, description: e.target.value })}
          />
        </label>
        <label className="form-group">
          Products
          <select
            multiple
            className="form-control"
            value={selectedProducts.map(String)}
            onChange={(e) =>
              setSelectedProducts(
                Array.from(e.target.selectedOptions).map(
                  (option) => option.value,
                ),
              )
            }
          >
            {products.map((product) => (
              <option key={product.id} value={product.id}>
                {product.name}
              </option>
            ))}
          </select>
        </label>
        <div className="form-row">
          <label className="form-group">
            Sort
            <input
              className="form-control"
              type="number"
              value={form.sortOrder}
              onChange={(e) => setForm({ ...form, sortOrder: e.target.value })}
            />
          </label>
          <label>
            <input
              type="checkbox"
              checked={form.isActive}
              onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
            />{" "}
            Active
          </label>
        </div>
      </fieldset>
    </form>
  );
}
