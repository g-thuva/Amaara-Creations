import { blank } from "@/features/admin/features/collections/collectionsForm.js";
export function CollectionForm({
  products,
  form,
  setForm,
  editing,
  setEditing,
  selectedProducts,
  setSelectedProducts,
  save,
  pending,
}) {
  return (
    <div className="card">
      <div className="card-header">
        <h3 className="card-title">
          {editing ? "Edit Collection" : "Create Collection"}
        </h3>
      </div>
      <form className="add-product-form" onSubmit={save}>
        <div className="form-row">
          <label className="form-group">
            Name
            <input
              className="form-control"
              required
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
        <div className="form-actions">
          <button
            type="button"
            className="btn-cancel"
            onClick={() => {
              setForm(blank);
              setEditing(null);
              setSelectedProducts([]);
            }}
          >
            Clear
          </button>
          <button type="submit" className="btn-submit" disabled={pending}>
            Save
          </button>
        </div>
      </form>
    </div>
  );
}
