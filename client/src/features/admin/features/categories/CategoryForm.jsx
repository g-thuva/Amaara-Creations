import { blank } from "@/features/admin/features/categories/categoriesForm.js";
export function CategoryForm({
  items,
  form,
  setForm,
  editing,
  setEditing,
  save,
  pending,
}) {
  return (
    <div className="card">
      <div className="card-header">
        <h3 className="card-title">
          {editing ? "Edit Category" : "Create Category"}
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
        <div className="form-row">
          <label className="form-group">
            Parent
            <select
              className="form-control"
              value={form.parentCategoryId || ""}
              onChange={(e) =>
                setForm({ ...form, parentCategoryId: e.target.value })
              }
            >
              <option value="">None</option>
              {items
                .filter((item) => item.id !== editing?.id)
                .map((item) => (
                  <option key={item.id} value={item.id}>
                    {item.name}
                  </option>
                ))}
            </select>
          </label>
          <label className="form-group">
            Sort
            <input
              className="form-control"
              type="number"
              value={form.sortOrder}
              onChange={(e) => setForm({ ...form, sortOrder: e.target.value })}
            />
          </label>
        </div>
        <label>
          <input
            type="checkbox"
            checked={form.isActive}
            onChange={(e) => setForm({ ...form, isActive: e.target.checked })}
          />{" "}
          Active
        </label>
        <div className="form-actions">
          <button
            type="button"
            className="btn-cancel"
            onClick={() => {
              setForm(blank);
              setEditing(null);
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
